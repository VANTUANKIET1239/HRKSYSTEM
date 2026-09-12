using Core.Common.Repositories;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;
using GAME.Domain.Entities;
using GAME.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace GAME.Infrastructure.Services
{
    public class EquipmentEnhancementService : IEquipmentEnhancementService
    {
        private readonly IUnitOfWork<GameDbContext> _unitOfWork;
        private readonly IGamePlayerService _gamePlayerService;
        private readonly IItemStatCalculationService _statCalculationService;
        private readonly ILogger<EquipmentEnhancementService> _logger;

        public EquipmentEnhancementService(
            IUnitOfWork<GameDbContext> unitOfWork,
            IGamePlayerService gamePlayerService,
            IItemStatCalculationService statCalculationService,
            ILogger<EquipmentEnhancementService> logger)
        {
            _unitOfWork = unitOfWork;
            _gamePlayerService = gamePlayerService;
            _statCalculationService = statCalculationService;
            _logger = logger;
        }

        public async Task<EnhancementConfigResponseDto> GetEnhancementConfigsAsync(CancellationToken cancellationToken = default)
        {
            var levelConfigs = await _unitOfWork.ReadOnlyRepository<HrkEnhancementLevelConfig>().Query()
                .OrderBy(c => c.CurrentLevel)
                .Select(c => new EnhancementLevelConfigDto
                {
                    CurrentLevel = c.CurrentLevel,
                    NextLevel = c.NextLevel,
                    BaseSuccessRate = c.BaseSuccessRate,
                    GoldCost = c.GoldCost,
                    FailureDropLevels = c.FailureDropLevels,
                    MaxStoneSlots = c.MaxStoneSlots
                })
                .ToListAsync(cancellationToken);

            var materials = await _unitOfWork.ReadOnlyRepository<HrkEnhancementMaterial>().Query()
                .Include(m => m.ItemTemplate).ThenInclude(t => t.Rarity)
                .Select(m => new EnhancementMaterialDto
                {
                    ItemTemplateId = m.ItemTemplateId,
                    Code = m.ItemTemplate.Code,
                    Name = m.ItemTemplate.Name,
                    Icon = m.ItemTemplate.Icon,
                    ImagePath = m.ItemTemplate.ImagePath,
                    RarityCode = m.ItemTemplate.Rarity.Code,
                    RarityName = m.ItemTemplate.Rarity.Name,
                    RarityColorHex = m.ItemTemplate.Rarity.ColorHex,
                    MaterialType = m.MaterialType,
                    SuccessRateBonus = m.SuccessRateBonus,
                    PreventLevelDrop = m.PreventLevelDrop,
                    Description = m.ItemTemplate.Description
                })
                .ToListAsync(cancellationToken);

            return new EnhancementConfigResponseDto
            {
                LevelConfigs = levelConfigs,
                Materials = materials
            };
        }

        public async Task<EnhanceEquipmentResultDto> EnhanceEquipmentAsync(
            string userId,
            EnhanceEquipmentRequestDto request,
            CancellationToken cancellationToken = default)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            // 1. Idempotency Check (Chống gửi trùng lặp theo RequestId)
            var existingHistory = await _unitOfWork.ReadOnlyRepository<HrkEquipmentEnhancementHistory>().Query()
                .Include(h => h.ItemTemplate).ThenInclude(t => t.Attributes).ThenInclude(a => a.AttributeType)
                .FirstOrDefaultAsync(h => h.RequestId == request.RequestId, cancellationToken);

            if (existingHistory != null)
            {
                _logger.LogInformation("Idempotent enhancement request detected for RequestId={RequestId}. Returning cached result.", request.RequestId);

                var cachedStats = _statCalculationService.CalculateCurrentStats(existingHistory.ItemTemplate, existingHistory.NewEnhancement, 0);

                return new EnhanceEquipmentResultDto
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
            }

            // 2. Validate Player & Wallet
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

            // 3. Validate Equipment
            var equipment = await _unitOfWork.Repository<HrkPlayerInventory>().Query()
                .Include(i => i.ItemTemplate).ThenInclude(t => t.Category)
                .Include(i => i.ItemTemplate).ThenInclude(t => t.Attributes).ThenInclude(a => a.AttributeType)
                .FirstOrDefaultAsync(i => i.Id == request.InventoryItemId && i.PlayerId == player.Id && i.IsActive, cancellationToken);

            if (equipment == null)
            {
                throw new KeyNotFoundException("Không tìm thấy trang bị cần cường hóa trong hành trang.");
            }

            if (equipment.ItemTemplate.Category == null || !equipment.ItemTemplate.Category.IsEquipment)
            {
                throw new InvalidOperationException("Vật phẩm được chọn không phải là trang bị có thể cường hóa.");
            }

            if (equipment.IsLocked)
            {
                throw new InvalidOperationException("Trang bị đang bị khóa, vui lòng mở khóa trước khi cường hóa.");
            }

            if (equipment.Enhancement >= 15)
            {
                throw new InvalidOperationException("Trang bị đã đạt cấp cường hóa tối đa (+15). Không thể cường hóa thêm.");
            }

            // 4. Fetch Level Configuration
            var levelConfig = await _unitOfWork.ReadOnlyRepository<HrkEnhancementLevelConfig>().Query()
                .FirstOrDefaultAsync(c => c.CurrentLevel == equipment.Enhancement, cancellationToken);

            if (levelConfig == null)
            {
                throw new InvalidOperationException($"Chưa có cấu hình cường hóa cho cấp độ +{equipment.Enhancement}.");
            }

            // 5. Validate Gold Cost
            if (wallet.Gold < levelConfig.GoldCost)
            {
                throw new InvalidOperationException($"Không đủ Vàng để cường hóa. Yêu cầu: {levelConfig.GoldCost:N0} Vàng (Hiện có: {wallet.Gold:N0} Vàng).");
            }

            // 6. Validate Enhancement Stones
            var stoneIds = request.StoneInventoryItemIds ?? new List<long>();
            if (stoneIds.Count > levelConfig.MaxStoneSlots)
            {
                throw new InvalidOperationException($"Chỉ được sử dụng tối đa {levelConfig.MaxStoneSlots} đá cường hóa cho một lần thử.");
            }

            var stoneGroups = stoneIds.GroupBy(id => id).ToDictionary(g => g.Key, g => g.Count());
            var stoneInventories = new List<HrkPlayerInventory>();
            var consumedStonesList = new List<ConsumedMaterialItemDto>();
            decimal totalStoneBonus = 0;

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
                        throw new InvalidOperationException($"Số lượng đá cường hóa '{stoneInv?.ItemTemplate.Name ?? "ID: " + invId}' trong túi không đủ ({stoneInv?.Count ?? 0}/{requiredQty}).");
                    }

                    if (!stoneConfigs.TryGetValue(stoneInv.ItemTemplateId, out var matConfig) || matConfig.MaterialType != "STONE")
                    {
                        throw new InvalidOperationException($"Vật phẩm '{stoneInv.ItemTemplate.Name}' không phải là đá cường hóa hợp lệ.");
                    }

                    totalStoneBonus += matConfig.SuccessRateBonus * requiredQty;
                    consumedStonesList.Add(new ConsumedMaterialItemDto
                    {
                        ItemTemplateId = stoneInv.ItemTemplateId,
                        Code = stoneInv.ItemTemplate.Code,
                        Name = stoneInv.ItemTemplate.Name,
                        Quantity = requiredQty,
                        SuccessRateBonus = matConfig.SuccessRateBonus
                    });
                }
            }

            // 7. Validate Charm
            HrkPlayerInventory? charmInventory = null;
            HrkEnhancementMaterial? charmConfig = null;
            ConsumedMaterialItemDto? consumedCharmDto = null;
            decimal charmBonus = 0;
            bool charmProtects = false;

            if (request.CharmInventoryItemId.HasValue && request.CharmInventoryItemId.Value > 0)
            {
                charmInventory = await _unitOfWork.Repository<HrkPlayerInventory>().Query()
                    .Include(i => i.ItemTemplate)
                    .FirstOrDefaultAsync(i => i.Id == request.CharmInventoryItemId.Value && i.PlayerId == player.Id && i.IsActive, cancellationToken);

                if (charmInventory == null || charmInventory.Count < 1)
                {
                    throw new InvalidOperationException("Không tìm thấy bùa hộ mệnh/may mắn trong hành trang hoặc đã hết số lượng.");
                }

                charmConfig = await _unitOfWork.ReadOnlyRepository<HrkEnhancementMaterial>().Query()
                    .FirstOrDefaultAsync(m => m.ItemTemplateId == charmInventory.ItemTemplateId, cancellationToken);

                if (charmConfig == null || charmConfig.MaterialType != "CHARM")
                {
                    throw new InvalidOperationException($"Vật phẩm '{charmInventory.ItemTemplate.Name}' không phải là bùa cường hóa hợp lệ.");
                }

                charmBonus = charmConfig.SuccessRateBonus;
                charmProtects = charmConfig.PreventLevelDrop;
                consumedCharmDto = new ConsumedMaterialItemDto
                {
                    ItemTemplateId = charmInventory.ItemTemplateId,
                    Code = charmInventory.ItemTemplate.Code,
                    Name = charmInventory.ItemTemplate.Name,
                    Quantity = 1,
                    SuccessRateBonus = charmConfig.SuccessRateBonus
                };
            }

            // 8. Calculate Final Rate (Capped at 100% = 1.0000)
            decimal baseSuccessRate = levelConfig.BaseSuccessRate;
            decimal finalSuccessRate = Math.Min(1.0000m, baseSuccessRate + totalStoneBonus + charmBonus);

            // 9. Authoritative Server RNG Roll
            int rollInt = RandomNumberGenerator.GetInt32(0, 10000);
            decimal rollDecimal = rollInt / 10000.0m;
            bool isSuccess = rollDecimal < finalSuccessRate;

            int oldEnhancement = equipment.Enhancement;
            int targetEnhancement = oldEnhancement + 1;
            int newEnhancement;
            int actualDropLevels;
            bool wasProtected;

            if (isSuccess)
            {
                newEnhancement = oldEnhancement + 1;
                actualDropLevels = 0;
                wasProtected = false;
            }
            else
            {
                if (charmProtects)
                {
                    actualDropLevels = 0;
                    wasProtected = true;
                    newEnhancement = oldEnhancement;
                }
                else
                {
                    actualDropLevels = levelConfig.FailureDropLevels;
                    wasProtected = false;
                    newEnhancement = Math.Max(0, oldEnhancement - actualDropLevels);
                }
            }

            await _unitOfWork.ExecuteStrategyAsync(async () =>
            {
                // 10. Atomic Database Transaction
                await _unitOfWork.BeginTransactionAsync();
                try
                {
                    // A. Update Equipment Enhancement Level and Snapshot CurrentStats
                    equipment.Enhancement = newEnhancement;
                    var currentStatsJson = _statCalculationService.CalculateCurrentStatsJson(equipment.ItemTemplate, newEnhancement, equipment.Stars);
                    equipment.CurrentStats = currentStatsJson;
                    equipment.UpdatedOn = DateTime.UtcNow;
                    _unitOfWork.Repository<HrkPlayerInventory>().Update(equipment);

                    // B. Deduct Gold
                    wallet.Gold -= levelConfig.GoldCost;
                    wallet.UpdatedOn = DateTime.UtcNow;
                    _unitOfWork.Repository<HrkPlayerWallet>().Update(wallet);

                    // C. Consume Stones (Decrement Count or Soft Delete)
                    foreach (var (invId, requiredQty) in stoneGroups)
                    {
                        var stoneInv = stoneInventories.First(s => s.Id == invId);
                        stoneInv.Count -= requiredQty;
                        if (stoneInv.Count <= 0)
                        {
                            stoneInv.Count = 0;
                            stoneInv.IsActive = false;
                        }
                        stoneInv.UpdatedOn = DateTime.UtcNow;
                        _unitOfWork.Repository<HrkPlayerInventory>().Update(stoneInv);
                    }

                    // D. Consume Charm (if used)
                    if (charmInventory != null)
                    {
                        charmInventory.Count -= 1;
                        if (charmInventory.Count <= 0)
                        {
                            charmInventory.Count = 0;
                            charmInventory.IsActive = false;
                        }
                        charmInventory.UpdatedOn = DateTime.UtcNow;
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
                        RequestId = request.RequestId,
                        PlayerId = player.Id,
                        PlayerInventoryId = equipment.Id,
                        ItemTemplateId = equipment.ItemTemplateId,
                        OldEnhancement = oldEnhancement,
                        TargetEnhancement = targetEnhancement,
                        NewEnhancement = newEnhancement,
                        BaseSuccessRate = baseSuccessRate,
                        StoneBonusRate = totalStoneBonus,
                        CharmBonusRate = charmBonus,
                        FinalSuccessRate = finalSuccessRate,
                        IsSuccess = isSuccess,
                        FailureDropLevels = actualDropLevels,
                        WasLevelProtected = wasProtected,
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
                    _logger.LogError(ex, "Transaction failed and was rolled back during enhancement attempt. RequestId={RequestId}", request.RequestId);
                    throw;
                }
            });

        
            // 11. Build Response DTO
            var calculatedStats = _statCalculationService.CalculateCurrentStats(equipment.ItemTemplate, newEnhancement, equipment.Stars);

            return new EnhanceEquipmentResultDto
            {
                RequestId = request.RequestId,
                Success = isSuccess,
                OldEnhancement = oldEnhancement,
                TargetEnhancement = targetEnhancement,
                NewEnhancement = newEnhancement,
                BaseSuccessRate = baseSuccessRate,
                StoneBonusRate = totalStoneBonus,
                CharmBonusRate = charmBonus,
                FinalSuccessRate = finalSuccessRate,
                WasLevelProtected = wasProtected,
                CurrentStats = calculatedStats,
                CurrentStatsJson = equipment.CurrentStats,
                Consumed = new ConsumedResourcesDto
                {
                    Gold = levelConfig.GoldCost,
                    Stones = consumedStonesList,
                    Charm = consumedCharmDto
                },
                Message = isSuccess
                    ? $"CƯỜNG HÓA THÀNH CÔNG (+{oldEnhancement} → +{newEnhancement})"
                    : (wasProtected
                        ? $"CƯỜNG HÓA THẤT BẠI (+{oldEnhancement} → +{newEnhancement}). Bùa Hộ Mệnh đã bảo vệ cấp độ!"
                        : $"CƯỜNG HÓA THẤT BẠI (+{oldEnhancement} → +{newEnhancement})")
            };
        }
    }
}
