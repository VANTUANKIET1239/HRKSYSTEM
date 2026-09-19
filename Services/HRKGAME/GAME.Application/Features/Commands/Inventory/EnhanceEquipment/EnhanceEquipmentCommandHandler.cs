using Core.Common.CQRS;
using Core.Common.Entity.MyCompany.Shared.Responses;
using Core.Common.Repositories;
using CoreEngine.CQRS;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;
using GAME.Domain.Entities;
using GAME.Domain.Exceptions;
using GAME.Domain.Services;
using GAME.Domain.ValueObjects;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace GAME.Application.Features.Commands.Inventory.EnhanceEquipment
{
    public class EnhanceEquipmentCommandHandler : HRKBaseCommand, ICommandHandler<EnhanceEquipmentCommand, BaseResponse<EnhanceEquipmentResultDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IGamePlayerService _gamePlayerService;
        private readonly IItemStatCalculationService _statCalculationService;
        private readonly IEquipmentEnhancementDomainService _enhancementDomainService;
        private readonly ILogger<EnhanceEquipmentCommandHandler> _logger;

        public EnhanceEquipmentCommandHandler(
            IUnitOfWork unitOfWork,
            IGamePlayerService gamePlayerService,
            IItemStatCalculationService statCalculationService,
            IEquipmentEnhancementDomainService enhancementDomainService,
            IHttpContextAccessor httpContextAccessor,
            ILogger<EnhanceEquipmentCommandHandler> logger)
            : base(httpContextAccessor)
        {
            _unitOfWork = unitOfWork;
            _gamePlayerService = gamePlayerService;
            _statCalculationService = statCalculationService;
            _enhancementDomainService = enhancementDomainService;
            _logger = logger;
        }

        public async Task<BaseResponse<EnhanceEquipmentResultDto>> Handle(EnhanceEquipmentCommand command, CancellationToken cancellationToken)
        {
            var userId = GetUserId();
            var req = command.Request;

            if (req == null)
            {
                return BaseResponse<EnhanceEquipmentResultDto>.FailResponse("Yêu cầu cường hóa không hợp lệ (Request is null).", statusCode: 400);
            }

            _logger.LogInformation("EnhancementAttemptStarted: UserId={UserId}, RequestId={RequestId}, InventoryItemId={InventoryItemId}, StonesCount={StonesCount}, HasCharm={HasCharm}",
                userId, req.RequestId, req.InventoryItemId, req.StoneInventoryItemIds?.Count ?? 0, req.CharmInventoryItemId.HasValue);

            try
            {
                // 1. Idempotency Check
                var existingHistory = await _unitOfWork.ReadOnlyRepository<HrkEquipmentEnhancementHistory>().Query()
                    .Include(h => h.ItemTemplate).ThenInclude(t => t.Attributes).ThenInclude(a => a.AttributeType)
                    .FirstOrDefaultAsync(h => h.RequestId == req.RequestId, cancellationToken);

                if (existingHistory != null)
                {
                    _logger.LogInformation("Idempotent enhancement request detected for RequestId={RequestId}. Returning cached result.", req.RequestId);

                    var cachedStats = _statCalculationService.CalculateCurrentStats(existingHistory.ItemTemplate, existingHistory.NewEnhancement, 0);

                    var cachedDto = new EnhanceEquipmentResultDto
                    {
                        RequestId = existingHistory.RequestId,
                        Success = existingHistory.IsSuccess,
                        OldEnhancement = existingHistory.OldEnhancement,
                        TargetEnhancement = existingHistory.TargetEnhancement,
                        NewEnhancement = existingHistory.NewEnhancement,
                        BaseSuccessRate = existingHistory.BaseSuccessRate,
                        StoneBonusRate = existingHistory.StoneBonusRate,
                        CharmBonusRate = existingHistory.CharmBonusRate,
                        FinalSuccessRate = existingHistory.FinalSuccessRate,
                        WasLevelProtected = existingHistory.WasLevelProtected,
                        CurrentStats = cachedStats,
                        CurrentStatsJson = _statCalculationService.CalculateCurrentStatsJson(existingHistory.ItemTemplate, existingHistory.NewEnhancement, 0),
                        Consumed = new ConsumedResourcesDto { Gold = existingHistory.GoldCost },
                        Message = existingHistory.IsSuccess ? "Cường hóa thành công (Yêu cầu trùng lặp)." : "Cường hóa thất bại (Yêu cầu trùng lặp)."
                    };

                    return BaseResponse<EnhanceEquipmentResultDto>.SuccessResponse(cachedDto, cachedDto.Message);
                }

                // 2. Load Player & Wallet
                var player = await _gamePlayerService.GetPlayerByUserIdAsync(userId, cancellationToken);
                if (player == null)
                {
                    throw new KeyNotFoundException("Không tìm thấy thông tin người chơi.");
                }

                var wallet = await _gamePlayerService.GetWalletByPlayerIdAsync(player.Id, cancellationToken);
                if (wallet == null)
                {
                    throw new KeyNotFoundException("Không tìm thấy thông tin ví của người chơi.");
                }

                // 3. Load Equipment Aggregate & Invariants
                var equipment = await _unitOfWork.Repository<HrkPlayerInventory>().Query()
                    .Include(i => i.ItemTemplate).ThenInclude(t => t.Category)
                    .Include(i => i.ItemTemplate).ThenInclude(t => t.Attributes).ThenInclude(a => a.AttributeType)
                    .FirstOrDefaultAsync(i => i.Id == req.InventoryItemId && i.PlayerId == player.Id && i.IsActive, cancellationToken);

                if (equipment == null)
                {
                    throw new KeyNotFoundException("Không tìm thấy trang bị cần cường hóa trong hành trang.");
                }

                // Protect Aggregate Invariants via Entity Method
                equipment.EnsureCanBeEnhanced();

                // 4. Fetch Level Configuration
                var levelConfig = await _unitOfWork.ReadOnlyRepository<HrkEnhancementLevelConfig>().Query()
                    .FirstOrDefaultAsync(c => c.CurrentLevel == equipment.Enhancement, cancellationToken);

                if (levelConfig == null)
                {
                    throw new InvalidOperationException($"Chưa có cấu hình cường hóa cho cấp độ +{equipment.Enhancement}.");
                }

                // 5. Validate Gold Cost using Wallet Domain Method
                if (!wallet.HasEnoughGold(levelConfig.GoldCost))
                {
                    throw new InvalidOperationException($"Không đủ Vàng để cường hóa. Yêu cầu: {levelConfig.GoldCost:N0} Vàng (Hiện có: {wallet.Gold:N0} Vàng).");
                }

                // 6. Validate & Load Enhancement Materials
                var stoneIds = req.StoneInventoryItemIds ?? new List<long>();
                var stoneGroups = stoneIds.GroupBy(id => id).ToDictionary(g => g.Key, g => g.Count());
                var stoneInventories = new List<HrkPlayerInventory>();
                var stoneMaterialItems = new List<StoneMaterialItem>();
                var consumedStonesList = new List<ConsumedMaterialItemDto>();

                if (stoneGroups.Any())
                {
                    var groupKeys = stoneGroups.Keys.ToList();
                    stoneInventories = await _unitOfWork.Repository<HrkPlayerInventory>().Query()
                        .Include(i => i.ItemTemplate)
                        .Where(i => groupKeys.Contains(i.Id) && i.PlayerId == player.Id && i.IsActive)
                        .ToListAsync(cancellationToken);

                    var templateIds = stoneInventories.Select(s => s.ItemTemplateId).Distinct().ToList();
                    var stoneConfigs = await _unitOfWork.ReadOnlyRepository<HrkEnhancementMaterial>().Query()
                        .Where(m => templateIds.Contains(m.ItemTemplateId))
                        .ToDictionaryAsync(m => m.ItemTemplateId, cancellationToken);

                    foreach (var (invId, requiredQty) in stoneGroups)
                    {
                        var stoneInv = stoneInventories.FirstOrDefault(s => s.Id == invId);
                        if (stoneInv == null || stoneInv.Count < requiredQty)
                        {
                            throw new InvalidOperationException($"Số lượng đá cường hóa '{stoneInv?.ItemTemplate?.Name ?? "ID: " + invId}' trong túi không đủ ({stoneInv?.Count ?? 0}/{requiredQty}).");
                        }

                        if (!stoneConfigs.TryGetValue(stoneInv.ItemTemplateId, out var matConfig) || matConfig.MaterialType != "STONE")
                        {
                            throw new InvalidOperationException($"Vật phẩm '{stoneInv.ItemTemplate?.Name}' không phải là đá cường hóa hợp lệ.");
                        }

                        stoneMaterialItems.Add(new StoneMaterialItem(
                            stoneInv.Id,
                            stoneInv.ItemTemplateId,
                            stoneInv.ItemTemplate?.Code ?? "",
                            stoneInv.ItemTemplate?.Name ?? "",
                            requiredQty,
                            matConfig.SuccessRateBonus));

                        consumedStonesList.Add(new ConsumedMaterialItemDto
                        {
                            ItemTemplateId = stoneInv.ItemTemplateId,
                            Code = stoneInv.ItemTemplate?.Code ?? "",
                            Name = stoneInv.ItemTemplate?.Name ?? "",
                            Quantity = requiredQty,
                            SuccessRateBonus = matConfig.SuccessRateBonus
                        });
                    }
                }

                // 7. Validate Charm
                HrkPlayerInventory? charmInventory = null;
                CharmMaterialItem? charmMaterialItem = null;
                ConsumedMaterialItemDto? consumedCharmDto = null;

                if (req.CharmInventoryItemId.HasValue && req.CharmInventoryItemId.Value > 0)
                {
                    charmInventory = await _unitOfWork.Repository<HrkPlayerInventory>().Query()
                        .Include(i => i.ItemTemplate)
                        .FirstOrDefaultAsync(i => i.Id == req.CharmInventoryItemId.Value && i.PlayerId == player.Id && i.IsActive, cancellationToken);

                    if (charmInventory == null || charmInventory.Count < 1)
                    {
                        throw new InvalidOperationException("Không tìm thấy bùa hộ mệnh/may mắn trong hành trang hoặc đã hết số lượng.");
                    }

                    var charmConfig = await _unitOfWork.ReadOnlyRepository<HrkEnhancementMaterial>().Query()
                        .FirstOrDefaultAsync(m => m.ItemTemplateId == charmInventory.ItemTemplateId, cancellationToken);

                    if (charmConfig == null || charmConfig.MaterialType != "CHARM")
                    {
                        throw new InvalidOperationException($"Vật phẩm '{charmInventory.ItemTemplate?.Name}' không phải là bùa cường hóa hợp lệ.");
                    }

                    charmMaterialItem = new CharmMaterialItem(
                        charmInventory.Id,
                        charmInventory.ItemTemplateId,
                        charmInventory.ItemTemplate?.Code ?? "",
                        charmInventory.ItemTemplate?.Name ?? "",
                        charmConfig.SuccessRateBonus,
                        charmConfig.PreventLevelDrop);

                    consumedCharmDto = new ConsumedMaterialItemDto
                    {
                        ItemTemplateId = charmInventory.ItemTemplateId,
                        Code = charmInventory.ItemTemplate?.Code ?? "",
                        Name = charmInventory.ItemTemplate?.Name ?? "",
                        Quantity = 1,
                        SuccessRateBonus = charmConfig.SuccessRateBonus
                    };
                }

                // 8. Build Value Object EnhancementMaterials (Enforces max stone slots invariant)
                var materialsVo = new EnhancementMaterials(stoneMaterialItems, charmMaterialItem, levelConfig.MaxStoneSlots);

                // 9. Execute Domain Service (Pure Domain Rules + RNG + Aggregate State Transition)
                // Option A: Enhancement roll occurs before retryable transactional delegate
                var executionResult = _enhancementDomainService.ExecuteAttempt(equipment, levelConfig, materialsVo);

                // 10. Atomic Database Transaction (Application Layer Orchestration)
                await _unitOfWork.ExecuteStrategyAsync(async () =>
                {
                    await _unitOfWork.BeginTransactionAsync();
                    try
                    {
                        // A. Recalculate and update Equipment Snapshot Stats cache
                        var currentStatsJson = _statCalculationService.CalculateCurrentStatsJson(
                            equipment.ItemTemplate,
                            executionResult.NewEnhancement,
                            equipment.Stars);

                        equipment.UpdateCurrentStatsCache(currentStatsJson);
                        _unitOfWork.Repository<HrkPlayerInventory>().Update(equipment);

                        // B. Deduct Gold via Wallet Domain Method
                        wallet.DeductGold(levelConfig.GoldCost);
                        _unitOfWork.Repository<HrkPlayerWallet>().Update(wallet);

                        // C. Consume Stones via Inventory Domain Method
                        foreach (var (invId, requiredQty) in stoneGroups)
                        {
                            var stoneInv = stoneInventories.First(s => s.Id == invId);
                            stoneInv.ConsumeQuantity(requiredQty);
                            _unitOfWork.Repository<HrkPlayerInventory>().Update(stoneInv);
                        }

                        // D. Consume Charm via Inventory Domain Method (if used)
                        if (charmInventory != null)
                        {
                            charmInventory.ConsumeQuantity(1);
                            _unitOfWork.Repository<HrkPlayerInventory>().Update(charmInventory);
                        }

                        // E. Save Business Audit Log (HRK_EquipmentEnhancementHistory)
                        var usedMaterialsSnapshot = new
                        {
                            stones = consumedStonesList,
                            charm = consumedCharmDto
                        };
                        var usedMaterialsJson = JsonSerializer.Serialize(usedMaterialsSnapshot);

                        var historyRecord = new HrkEquipmentEnhancementHistory
                        {
                            RequestId = req.RequestId,
                            PlayerId = player.Id,
                            PlayerInventoryId = equipment.Id,
                            ItemTemplateId = equipment.ItemTemplateId,
                            OldEnhancement = executionResult.OldEnhancement,
                            TargetEnhancement = executionResult.TargetEnhancement,
                            NewEnhancement = executionResult.NewEnhancement,
                            BaseSuccessRate = executionResult.BaseSuccessRate,
                            StoneBonusRate = executionResult.StoneBonusRate,
                            CharmBonusRate = executionResult.CharmBonusRate,
                            FinalSuccessRate = executionResult.FinalSuccessRate,
                            IsSuccess = executionResult.IsSuccess,
                            FailureDropLevels = executionResult.FailureDropLevels,
                            WasLevelProtected = executionResult.WasLevelProtected,
                            GoldCost = levelConfig.GoldCost,
                            UsedMaterialsJson = usedMaterialsJson,
                            CreatedOn = DateTime.UtcNow
                        };

                        await _unitOfWork.Repository<HrkEquipmentEnhancementHistory>().AddAsync(historyRecord);

                        // Commit everything atomically
                        await _unitOfWork.SaveChangesAsync(cancellationToken);
                        await _unitOfWork.CommitTransactionAsync();
                    }
                    catch (Exception ex)
                    {
                        await _unitOfWork.RollbackTransactionAsync();
                        _logger.LogError(ex, "Transaction failed and was rolled back during enhancement attempt. RequestId={RequestId}", req.RequestId);
                        throw;
                    }
                });

                // 11. Build Response DTO
                var calculatedStats = _statCalculationService.CalculateCurrentStats(equipment.ItemTemplate, executionResult.NewEnhancement, equipment.Stars);

                var responseDto = new EnhanceEquipmentResultDto
                {
                    RequestId = req.RequestId,
                    Success = executionResult.IsSuccess,
                    OldEnhancement = executionResult.OldEnhancement,
                    TargetEnhancement = executionResult.TargetEnhancement,
                    NewEnhancement = executionResult.NewEnhancement,
                    BaseSuccessRate = executionResult.BaseSuccessRate,
                    StoneBonusRate = executionResult.StoneBonusRate,
                    CharmBonusRate = executionResult.CharmBonusRate,
                    FinalSuccessRate = executionResult.FinalSuccessRate,
                    WasLevelProtected = executionResult.WasLevelProtected,
                    CurrentStats = calculatedStats,
                    CurrentStatsJson = equipment.CurrentStats,
                    Consumed = new ConsumedResourcesDto
                    {
                        Gold = levelConfig.GoldCost,
                        Stones = consumedStonesList,
                        Charm = consumedCharmDto
                    },
                    Message = executionResult.IsSuccess
                        ? $"CƯỜNG HÓA THÀNH CÔNG (+{executionResult.OldEnhancement} → +{executionResult.NewEnhancement})"
                        : (executionResult.WasLevelProtected
                            ? $"CƯỜNG HÓA THẤT BẠI (+{executionResult.OldEnhancement} → +{executionResult.NewEnhancement}). Bùa Hộ Mệnh đã bảo vệ cấp độ!"
                            : $"CƯỜNG HÓA THẤT BẠI (+{executionResult.OldEnhancement} → +{executionResult.NewEnhancement})")
                };

                if (responseDto.Success)
                {
                    _logger.LogInformation("EnhancementAttemptSucceeded: UserId={UserId}, RequestId={RequestId}, InventoryItemId={InventoryItemId}, OldLevel={OldLevel}, NewLevel={NewLevel}, FinalRate={FinalRate:P2}",
                        userId, req.RequestId, req.InventoryItemId, responseDto.OldEnhancement, responseDto.NewEnhancement, responseDto.FinalSuccessRate);

                    return BaseResponse<EnhanceEquipmentResultDto>.SuccessResponse(responseDto, "Cường hóa trang bị thành công!");
                }
                else
                {
                    _logger.LogInformation("EnhancementAttemptFailed: UserId={UserId}, RequestId={RequestId}, InventoryItemId={InventoryItemId}, OldLevel={OldLevel}, NewLevel={NewLevel}, WasProtected={WasProtected}, FinalRate={FinalRate:P2}",
                        userId, req.RequestId, req.InventoryItemId, responseDto.OldEnhancement, responseDto.NewEnhancement, responseDto.WasLevelProtected, responseDto.FinalSuccessRate);

                    return BaseResponse<EnhanceEquipmentResultDto>.SuccessResponse(responseDto, responseDto.WasLevelProtected ? "Cường hóa thất bại, Bùa Hộ Mệnh đã bảo vệ cấp độ!" : "Cường hóa thất bại!");
                }
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning("EnhancementAttemptRejected: UserId={UserId}, RequestId={RequestId}, Reason={Reason}",
                    userId, req.RequestId, ex.Message);
                return BaseResponse<EnhanceEquipmentResultDto>.FailResponse(ex.Message, statusCode: 404);
            }
            catch (DomainException ex)
            {
                _logger.LogWarning("EnhancementAttemptRejectedDomain: UserId={UserId}, RequestId={RequestId}, ReasonCode={ReasonCode}, Reason={Reason}",
                    userId, req.RequestId, ex.ReasonCode, ex.Message);
                return BaseResponse<EnhanceEquipmentResultDto>.FailResponse(ex.Message, statusCode: 400);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("EnhancementAttemptRejected: UserId={UserId}, RequestId={RequestId}, Reason={Reason}",
                    userId, req.RequestId, ex.Message);
                return BaseResponse<EnhanceEquipmentResultDto>.FailResponse(ex.Message, statusCode: 400);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "EnhancementAttemptError: UserId={UserId}, RequestId={RequestId}, Error={Error}",
                    userId, req.RequestId, ex.Message);
                return BaseResponse<EnhanceEquipmentResultDto>.FailResponse("Đã xảy ra lỗi trong quá trình cường hóa trang bị: " + ex.Message, statusCode: 500);
            }
        }
    }
}
