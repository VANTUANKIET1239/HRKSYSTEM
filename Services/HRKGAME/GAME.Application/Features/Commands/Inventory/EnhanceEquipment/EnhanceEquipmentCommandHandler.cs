using GAME.Application.Templates;
using Core.Common.Errors;
using GAME.Application.Common;
using Core.Common.Entity.MyCompany.Shared.Responses;
using Core.Persistence.Repositories;
using Core.Application.CQRS;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;
using GAME.Application.Events;
using GAME.Domain.Entities;
using GAME.Domain.Services;
using GAME.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Oservability.Tracing;

namespace GAME.Application.Features.Commands.Inventory.EnhanceEquipment
{
    public class EnhanceEquipmentCommandHandler : AuthenticatedGameHandler, ICommandHandler<EnhanceEquipmentCommand, BaseResponse<EnhanceEquipmentResultDto>>
    {
        private readonly IGameTemplateCatalog _templates;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IGamePlayerService _gamePlayerService;
        private readonly IItemStatCalculationService _statCalculationService;
        private readonly IEquipmentEnhancementDomainService _enhancementDomainService;
        private readonly ILogger<EnhanceEquipmentCommandHandler> _logger;
        private readonly IPlayerActivityEvents _activityEvents;
        public EnhanceEquipmentCommandHandler(IUnitOfWork unitOfWork, IGamePlayerService gamePlayerService, IItemStatCalculationService statCalculationService, IEquipmentEnhancementDomainService enhancementDomainService, IGameRequestContext requestContext, ILogger<EnhanceEquipmentCommandHandler> logger, IPlayerActivityEvents activityEvents, IGameTemplateCatalog? templates = null) : base(requestContext)
        {
            _templates = templates ?? new DatabaseTemplateCatalog(unitOfWork);
            _unitOfWork = unitOfWork;
            _gamePlayerService = gamePlayerService;
            _statCalculationService = statCalculationService;
            _enhancementDomainService = enhancementDomainService;
            _logger = logger;
            _activityEvents = activityEvents;
        }

        public async Task<BaseResponse<EnhanceEquipmentResultDto>> Handle(EnhanceEquipmentCommand command, CancellationToken cancellationToken)
        {
            var userId = GetUserId();
            var req = command.Request;
            if (req == null)
            {
                return BaseResponse<EnhanceEquipmentResultDto>.FailResponse("Yêu cầu cường hóa không hợp lệ (Request is null).", statusCode: 400);
            }

            _logger.LogInformation("EnhancementAttemptStarted: UserId={UserId}, RequestId={RequestId}, InventoryItemId={InventoryItemId}, StonesCount={StonesCount}, HasCharm={HasCharm}", userId, req.RequestId, req.InventoryItemId, req.StoneInventoryItemIds?.Count ?? 0, req.CharmInventoryItemId.HasValue);
            {
                var itemTemplates = (await _templates.GetAllAsync<HrkItemTemplate>(cancellationToken)).ToDictionary(t => t.Id);
                var attributeTypes = (await _templates.GetAllAsync<HrkAttributeType>(cancellationToken)).ToDictionary(a => a.Id);
                // 1. Idempotency Check
                var existingHistory = await _unitOfWork.ReadOnlyRepository<HrkEquipmentEnhancementHistory>().Query()
                    .FirstOrDefaultAsync(h => h.RequestId == req.RequestId, cancellationToken);
                if (existingHistory != null)
                {
                    _logger.LogInformation("Idempotent enhancement request detected for RequestId={RequestId}. Returning cached result.", req.RequestId);
                    var cachedStats = _statCalculationService.CalculateCurrentStats(itemTemplates[existingHistory.ItemTemplateId], existingHistory.NewEnhancement, 0);
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
                        CurrentStatsJson = _statCalculationService.CalculateCurrentStatsJson(itemTemplates[existingHistory.ItemTemplateId], existingHistory.NewEnhancement, 0),
                        Consumed = new ConsumedResourcesDto
                        {
                            Gold = existingHistory.GoldCost
                        },
                        Message = existingHistory.IsSuccess ? "Cường hóa thành công (Yêu cầu trùng lặp)." : "Cường hóa thất bại (Yêu cầu trùng lặp)."
                    };
                    return BaseResponse<EnhanceEquipmentResultDto>.SuccessResponse(cachedDto, cachedDto.Message);
                }

                // 2. Load Player & Wallet
                var player = await _gamePlayerService.GetPlayerByUserIdAsync(userId, cancellationToken);
                if (player == null)
                {
                    throw new BusinessException("PLAYER_NOT_FOUND", "Không tìm thấy thông tin người chơi.", BusinessErrorKind.NotFound);
                }

                using var playerScope = _logger.BeginScope(new Dictionary<string, object?> { ["PlayerId"] = player.Id });
                var wallet = await _gamePlayerService.GetWalletByPlayerIdAsync(player.Id, cancellationToken);
                if (wallet == null)
                {
                    throw new BusinessException("PLAYER_WALLET_NOT_FOUND", "Không tìm thấy thông tin ví của người chơi.", BusinessErrorKind.NotFound);
                }

                // 3. Load Equipment Aggregate & Invariants
                var equipment = await _unitOfWork.Repository<HrkPlayerInventory>().Query()
                    .Include(i => i.Attributes)
                    .FirstOrDefaultAsync(i => i.Id == req.InventoryItemId && i.PlayerId == player.Id && i.IsActive, cancellationToken);
                if (equipment == null)
                {
                    throw new BusinessException("ITEM_NOT_FOUND", "Không tìm thấy trang bị cần cường hóa trong hành trang.", BusinessErrorKind.NotFound);
                }

                // Protect Aggregate Invariants via Entity Method
                var calculationEquipment = await TemplateHydration.InventoryViewAsync(_templates, equipment, cancellationToken);
                calculationEquipment.EnsureCanBeEnhanced();
                // 4. Fetch Level Configuration
                var levelConfig = (await _templates.GetAllAsync<HrkEnhancementLevelConfig>(cancellationToken))
                    .FirstOrDefault(c => c.CurrentLevel == equipment.Enhancement);
                if (levelConfig == null)
                {
                    throw new InvalidOperationException($"Chưa có cấu hình cường hóa cho cấp độ +{equipment.Enhancement}.");
                }

                // 5. Validate Gold Cost using Wallet Domain Method
                if (!wallet.HasEnoughGold(levelConfig.GoldCost))
                {
                    throw BusinessException.Localized("INSUFFICIENT_GOLD", $"Không đủ Vàng để cường hóa. Yêu cầu: {levelConfig.GoldCost:N0} Vàng (Hiện có: {wallet.Gold:N0} Vàng).");
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
                        .Where(i => groupKeys.Contains(i.Id) && i.PlayerId == player.Id && i.IsActive)
                        .ToListAsync(cancellationToken);
                    var templateIds = stoneInventories.Select(s => s.ItemTemplateId).Distinct().ToList();
                    var stoneConfigs = (await _templates.GetAllAsync<HrkEnhancementMaterial>(cancellationToken))
                        .Where(m => templateIds.Contains(m.ItemTemplateId))
                        .ToDictionary(m => m.ItemTemplateId);
                    foreach (var (invId, requiredQty) in stoneGroups)
                    {
                        var stoneInv = stoneInventories.FirstOrDefault(s => s.Id == invId);
                        var stoneTemplate = stoneInv == null ? null : itemTemplates.GetValueOrDefault(stoneInv.ItemTemplateId);
                        if (stoneInv == null || stoneInv.Count < requiredQty)
                        {
                            throw BusinessException.Localized("INSUFFICIENT_MATERIALS", $"Số lượng đá cường hóa '{stoneTemplate?.Name ?? "ID: " + invId}' trong túi không đủ ({stoneInv?.Count ?? 0}/{requiredQty}).", BusinessErrorKind.Conflict);
                        }

                        if (!stoneConfigs.TryGetValue(stoneInv.ItemTemplateId, out var matConfig) || matConfig.MaterialType != "STONE")
                        {
                            throw BusinessException.Localized("INVALID_ENHANCEMENT_STONE", $"Vật phẩm '{stoneTemplate?.Name}' không phải là đá cường hóa hợp lệ.", BusinessErrorKind.Conflict);
                        }

                        stoneMaterialItems.Add(new StoneMaterialItem(stoneInv.Id, stoneInv.ItemTemplateId, stoneTemplate?.Code ?? "", stoneTemplate?.Name ?? "", requiredQty, matConfig.SuccessRateBonus));
                        consumedStonesList.Add(new ConsumedMaterialItemDto { ItemTemplateId = stoneInv.ItemTemplateId, Code = stoneTemplate?.Code ?? "", Name = stoneTemplate?.Name ?? "", Quantity = requiredQty, SuccessRateBonus = matConfig.SuccessRateBonus });
                    }
                }

                // 7. Validate Charm
                HrkPlayerInventory? charmInventory = null;
                CharmMaterialItem? charmMaterialItem = null;
                ConsumedMaterialItemDto? consumedCharmDto = null;
                if (req.CharmInventoryItemId.HasValue && req.CharmInventoryItemId.Value > 0)
                {
                    charmInventory = await _unitOfWork.Repository<HrkPlayerInventory>().Query()
                        .FirstOrDefaultAsync(i => i.Id == req.CharmInventoryItemId.Value && i.PlayerId == player.Id && i.IsActive, cancellationToken);
                    if (charmInventory == null || charmInventory.Count < 1)
                    {
                        throw new BusinessException("ENHANCEMENT_CHARM_UNAVAILABLE", "Không tìm thấy bùa hộ mệnh/may mắn trong hành trang hoặc đã hết số lượng.", BusinessErrorKind.Conflict);
                    }

                    var charmTemplate = itemTemplates[charmInventory.ItemTemplateId];
                    var charmConfig = (await _templates.GetAllAsync<HrkEnhancementMaterial>(cancellationToken))
                        .FirstOrDefault(m => m.ItemTemplateId == charmInventory.ItemTemplateId);
                    if (charmConfig == null || charmConfig.MaterialType != "CHARM")
                    {
                        throw BusinessException.Localized("INVALID_ENHANCEMENT_CHARM", $"Vật phẩm '{charmTemplate?.Name}' không phải là bùa cường hóa hợp lệ.", BusinessErrorKind.Conflict);
                    }

                    charmMaterialItem = new CharmMaterialItem(charmInventory.Id, charmInventory.ItemTemplateId, charmTemplate?.Code ?? "", charmTemplate?.Name ?? "", charmConfig.SuccessRateBonus, charmConfig.PreventLevelDrop);
                    consumedCharmDto = new ConsumedMaterialItemDto
                    {
                        ItemTemplateId = charmInventory.ItemTemplateId,
                        Code = charmTemplate?.Code ?? "",
                        Name = charmTemplate?.Name ?? "",
                        Quantity = 1,
                        SuccessRateBonus = charmConfig.SuccessRateBonus
                    };
                }

                // 8. Build Value Object EnhancementMaterials (Enforces max stone slots invariant)
                var materialsVo = new EnhancementMaterials(stoneMaterialItems, charmMaterialItem, levelConfig.MaxStoneSlots);
                // 9. Execute Domain Service (Pure Domain Rules + RNG + Aggregate State Transition)
                // This roll belongs to the current transactional attempt. Rollback discards its result.
                using var operation = new GameOperation("inventory.enhancement");
                var executionResult = _enhancementDomainService.ExecuteAttempt(calculationEquipment, levelConfig, materialsVo);
                equipment.Enhancement = executionResult.NewEnhancement;
                equipment.UpdatedOn = calculationEquipment.UpdatedOn;
                // 10. Apply the attempt in the transaction owned by the ranking wrapper.

                // A. Recalculate and update Equipment Instance Attributes & Snapshot Stats cache
                decimal growthPercent = equipment.EnhancementGrowthPercent ?? 10.0m;
                int safeStars = Math.Max(0, equipment.Stars);
                decimal starMultiplier = 1.0m + (safeStars * 0.10m);
                if (equipment.Attributes != null && equipment.Attributes.Count > 0)
                {
                    foreach (var attr in equipment.Attributes)
                    {
                        bool isPercentage = attributeTypes.GetValueOrDefault(attr.AttributeTypeId)?.IsPercentage ?? false;
                        decimal mult = 1.0m + (executionResult.NewEnhancement * growthPercent / 100.0m);
                        attr.CurrentValue = isPercentage ? Math.Round(attr.BaseRolledValue * mult, 4) : Math.Round(attr.BaseRolledValue * mult * starMultiplier, 2);
                        attr.UpdatedOn = DateTime.UtcNow;
                        _unitOfWork.Repository<HrkPlayerInventoryAttribute>().Update(attr);
                    }
                }

                var currentStatsJson = _statCalculationService.CalculateCurrentStatsJson(calculationEquipment, executionResult.NewEnhancement);
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
                _activityEvents.Raise(new ItemEnhancedEvent(player.Id, userId, req.RequestId, equipment.Id, equipment.ItemTemplateId, executionResult.OldEnhancement, executionResult.NewEnhancement, executionResult.IsSuccess, levelConfig.GoldCost));
                // Commit everything atomically
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                // 11. Build Response DTO
                operation.Complete(executionResult.IsSuccess ? "success" : "roll_failure");
                var calculatedStats = _statCalculationService.CalculateCurrentStats(calculationEquipment, executionResult.NewEnhancement);
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
                    Message = executionResult.IsSuccess ? $"CƯỜNG HÓA THÀNH CÔNG (+{executionResult.OldEnhancement} → +{executionResult.NewEnhancement})" : (executionResult.WasLevelProtected ? $"CƯỜNG HÓA THẤT BẠI (+{executionResult.OldEnhancement} → +{executionResult.NewEnhancement}). Bùa Hộ Mệnh đã bảo vệ cấp độ!" : $"CƯỜNG HÓA THẤT BẠI (+{executionResult.OldEnhancement} → +{executionResult.NewEnhancement})"),
                    MessageKey = executionResult.IsSuccess ? "ENHANCEMENT_SUCCESS_DETAIL" : executionResult.WasLevelProtected ? "ENHANCEMENT_PROTECTED_DETAIL" : "ENHANCEMENT_FAILURE_DETAIL",
                    MessageArguments = new() { ["old"] = executionResult.OldEnhancement, ["next"] = executionResult.NewEnhancement }
                };
                if (responseDto.Success)
                {
                    _logger.LogInformation("EnhancementAttemptSucceeded: UserId={UserId}, RequestId={RequestId}, InventoryItemId={InventoryItemId}, OldLevel={OldLevel}, NewLevel={NewLevel}, FinalRate={FinalRate:P2}", userId, req.RequestId, req.InventoryItemId, responseDto.OldEnhancement, responseDto.NewEnhancement, responseDto.FinalSuccessRate);
                    return BaseResponse<EnhanceEquipmentResultDto>.SuccessResponse(responseDto, "Cường hóa trang bị thành công!");
                }
                else
                {
                    _logger.LogInformation("EnhancementAttemptFailed: UserId={UserId}, RequestId={RequestId}, InventoryItemId={InventoryItemId}, OldLevel={OldLevel}, NewLevel={NewLevel}, WasProtected={WasProtected}, FinalRate={FinalRate:P2}", userId, req.RequestId, req.InventoryItemId, responseDto.OldEnhancement, responseDto.NewEnhancement, responseDto.WasLevelProtected, responseDto.FinalSuccessRate);
                    return BaseResponse<EnhanceEquipmentResultDto>.SuccessResponse(responseDto, responseDto.WasLevelProtected ? "Cường hóa thất bại, Bùa Hộ Mệnh đã bảo vệ cấp độ!" : "Cường hóa thất bại!");
                }
            }
        }
    }
}
