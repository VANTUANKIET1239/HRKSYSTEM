using Core.Common.Repositories;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;
using GAME.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace GAME.Infrastructure.Services
{
    public class EquipmentInstanceFactory : IEquipmentInstanceFactory
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IRandomService _randomService;
        private readonly IItemStatCalculationService _statCalculationService;
        private readonly ICombatPowerService _combatPowerService;

        public EquipmentInstanceFactory(
            IUnitOfWork unitOfWork,
            IRandomService randomService,
            IItemStatCalculationService statCalculationService,
            ICombatPowerService combatPowerService)
        {
            _unitOfWork = unitOfWork;
            _randomService = randomService;
            _statCalculationService = statCalculationService;
            _combatPowerService = combatPowerService;
        }

        public async Task<EquipmentInstanceCreationResult> CreateAsync(
            long playerId,
            HrkItemTemplate template,
            EquipmentAcquisitionContext context,
            CancellationToken cancellationToken = default)
        {
            if (template == null)
            {
                throw new ArgumentNullException(nameof(template), "Item template không được để trống.");
            }

            // 1. Load active rarity roll config
            var rarityConfig = await _unitOfWork.ReadOnlyRepository<HrkEquipmentRarityRollConfig>().Query()
                .FirstOrDefaultAsync(c => c.RarityId == template.RarityId && c.IsActive, cancellationToken);

            if (rarityConfig == null)
            {
                throw new InvalidOperationException($"Chưa cấu hình tỷ lệ tăng trưởng cường hóa cho phẩm chất '{template.Rarity?.Name ?? template.RarityId.ToString()}'.");
            }

            // 2. Roll enhancement growth rate
            decimal growthPercent = _randomService.NextDecimal(
                rarityConfig.EnhancementGrowthMinPercent,
                rarityConfig.EnhancementGrowthMaxPercent,
                decimals: 2);

            // 3. Create HrkPlayerInventory entity
            var inventoryItem = new HrkPlayerInventory
            {
                PlayerId = playerId,
                ItemTemplateId = template.Id,
                Count = 1,
                Enhancement = 0,
                Stars = 0,
                IsEquipped = false,
                EquippedHeroId = null,
                IsLocked = false,
                IsActive = true,
                EnhancementGrowthPercent = growthPercent,
                EnhancementGrowthMinPercent = rarityConfig.EnhancementGrowthMinPercent,
                EnhancementGrowthMaxPercent = rarityConfig.EnhancementGrowthMaxPercent,
                AcquiredOn = DateTime.UtcNow,
                UpdatedOn = DateTime.UtcNow
            };

            // 4. Roll each attribute within [MinValue, MaxValue]
            var rolledAttributesDtoList = new List<EquipmentRolledAttributeDto>();
            var templateAttributes = template.Attributes ?? new List<HrkItemTemplateAttribute>();

            foreach (var templateAttr in templateAttributes)
            {
                var attrType = templateAttr.AttributeType;
                bool isPercentage = attrType?.IsPercentage ?? false;

                decimal minVal = templateAttr.MinValue ?? templateAttr.Value;
                decimal maxVal = templateAttr.MaxValue ?? templateAttr.Value;

                if (minVal > maxVal)
                {
                    throw new InvalidOperationException($"Cấu hình thuộc tính ID {templateAttr.Id} không hợp lệ: MinValue ({minVal}) lớn hơn MaxValue ({maxVal}).");
                }

                decimal rolledBaseValue;
                if (minVal == maxVal)
                {
                    rolledBaseValue = minVal;
                }
                else if (isPercentage)
                {
                    rolledBaseValue = _randomService.NextDecimal(minVal, maxVal, decimals: 4);
                }
                else
                {
                    // Thuộc tính số nguyên (HP, ATK, DEF, SPD, etc.)
                    int minInt = (int)Math.Round(minVal, MidpointRounding.AwayFromZero);
                    int maxInt = (int)Math.Round(maxVal, MidpointRounding.AwayFromZero);
                    rolledBaseValue = _randomService.Next(minInt, maxInt + 1);
                }

                // Tính RollQualityPercent
                decimal qualityPercent;
                if (maxVal > minVal)
                {
                    qualityPercent = Math.Round(((rolledBaseValue - minVal) / (maxVal - minVal)) * 100m, 2);
                    qualityPercent = Math.Clamp(qualityPercent, 0m, 100m);
                }
                else
                {
                    qualityPercent = 100.00m;
                }

                // Cấp 0 ban đầu: CurrentValue = BaseRolledValue
                decimal currentValue = rolledBaseValue;

                var inventoryAttr = new HrkPlayerInventoryAttribute
                {
                    PlayerInventory = inventoryItem,
                    AttributeTypeId = templateAttr.AttributeTypeId,
                    BaseRolledValue = rolledBaseValue,
                    CurrentValue = currentValue,
                    RollMinValue = minVal,
                    RollMaxValue = maxVal,
                    RollQualityPercent = qualityPercent,
                    CreatedOn = DateTime.UtcNow,
                    UpdatedOn = DateTime.UtcNow
                };

                inventoryItem.Attributes.Add(inventoryAttr);

                rolledAttributesDtoList.Add(new EquipmentRolledAttributeDto
                {
                    AttributeTypeId = templateAttr.AttributeTypeId,
                    AttributeCode = attrType?.Code ?? $"ATTR_{templateAttr.AttributeTypeId}",
                    AttributeName = attrType?.Name ?? attrType?.Code ?? "Thuộc tính",
                    IsPercentage = isPercentage,
                    BaseRolledValue = rolledBaseValue,
                    EnhancementValue = 0m,
                    CurrentValue = currentValue,
                    RollMinValue = minVal,
                    RollMaxValue = maxVal,
                    RollQualityPercent = qualityPercent,
                    DisplayOrder = attrType?.DisplayOrder ?? 0
                });
            }

            decimal overallRollPercent = rolledAttributesDtoList.Count > 0
                ? Math.Round(rolledAttributesDtoList.Average(a => a.RollQualityPercent), 1)
                : 100m;

            // 5. Calculate CurrentStats cache JSON
            var statsDict = _statCalculationService.CalculateCurrentStats(inventoryItem);
            inventoryItem.CurrentStats = JsonSerializer.Serialize(statsDict);

            // 6. Calculate Combat Power for this equipment
            var calculatedStatsDto = MapStatsDictToCalculatedStats(statsDict);
            int combatPower = await _combatPowerService.CalculateAsync(calculatedStatsDto, cancellationToken);

            // 7. Build complete DungeonDroppedEquipmentDto
            var droppedDto = new DungeonDroppedEquipmentDto
            {
                InventoryItemId = inventoryItem.Id, // Will be updated with generated ID after save
                ItemTemplateId = template.Id,
                Code = template.Code,
                Name = template.Name,
                ImagePath = template.ImagePath,
                RarityCode = template.Rarity?.Code ?? "COMMON",
                RarityName = template.Rarity?.Name ?? "Thông thường",
                RarityColorHex = template.Rarity?.ColorHex ?? "#9e9e9e",
                CategoryCode = template.Category?.Code ?? "EQUIPMENT",
                CategoryName = template.Category?.Name ?? "Trang bị",
                Count = 1,
                EnhancementGrowthPercent = growthPercent,
                EnhancementGrowthMinPercent = rarityConfig.EnhancementGrowthMinPercent,
                EnhancementGrowthMaxPercent = rarityConfig.EnhancementGrowthMaxPercent,
                OverallRollPercent = overallRollPercent,
                RolledAttributes = rolledAttributesDtoList,
                CurrentStats = statsDict,
                CombatPower = combatPower
            };

            return new EquipmentInstanceCreationResult
            {
                InventoryItem = inventoryItem,
                DroppedDto = droppedDto
            };
        }

        private static CalculatedStatsDto MapStatsDictToCalculatedStats(Dictionary<string, decimal> dict)
        {
            var stats = new CalculatedStatsDto();
            foreach (var kvp in dict)
            {
                decimal v = kvp.Value;
                switch (kvp.Key.ToUpperInvariant())
                {
                    case "HP": stats.Hp = (int)v; break;
                    case "ATK": case "PHYSICAL_ATK": stats.Atk = (int)v; break;
                    case "DEF": case "ARMOR": stats.Def = (int)v; break;
                    case "SPD": case "SPEED": stats.Spd = (int)v; break;
                    case "CRIT": case "CRIT_RATE": stats.Crit = v > 1 ? v / 100m : v; break;
                    case "CRIT_DMG": case "CRIT_DAMAGE": stats.CritDmg = v > 1 ? v / 100m : v; break;
                    case "LIFESTEAL": stats.Lifesteal = v > 1 ? v / 100m : v; break;
                    case "ACCURACY": stats.Accuracy = v > 1 ? v / 100m : v; break;
                    case "RESISTANCE": stats.Resistance = v > 1 ? v / 100m : v; break;
                    case "MAGIC_DAMAGE": case "MAGIC_ATK": case "MATK": stats.MagicDamage = (int)v; break;
                    case "MAGIC_RESISTANCE": case "MAGIC_RESIST": case "MRES": stats.MagicResistance = (int)v; break;
                }
            }
            return stats;
        }
    }
}
