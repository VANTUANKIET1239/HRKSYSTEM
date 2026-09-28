using GAME.Application.DTOs;
using GAME.Application.Interfaces;
using GAME.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace GAME.Infrastructure.Services
{
    public class HeroStatCalculationService : IHeroStatCalculationService
    {
        private readonly IItemStatCalculationService _itemStatCalculationService;
        private static readonly JsonSerializerOptions CaseInsensitiveOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public HeroStatCalculationService(IItemStatCalculationService itemStatCalculationService)
        {
            _itemStatCalculationService = itemStatCalculationService;
        }

        public HeroStatCalculationResult CalculateStats(HrkPlayerHero? ph, HrkPlayerEquipment? equipment)
        {
            var validItems = new List<HrkPlayerInventory>();
            if (ph != null && equipment != null && equipment.PlayerId == ph.PlayerId && equipment.HeroId == ph.Id)
            {
                AddIfValid(validItems, equipment.Weapon, equipment.WeaponId, ph.Id, ph.PlayerId);
                AddIfValid(validItems, equipment.Armor, equipment.ArmorId, ph.Id, ph.PlayerId);
                AddIfValid(validItems, equipment.Helmet, equipment.HelmetId, ph.Id, ph.PlayerId);
                AddIfValid(validItems, equipment.Boots, equipment.BootsId, ph.Id, ph.PlayerId);
                AddIfValid(validItems, equipment.Ring, equipment.RingId, ph.Id, ph.PlayerId);
                AddIfValid(validItems, equipment.Artifact, equipment.ArtifactId, ph.Id, ph.PlayerId);
            }

            return CalculateStats(ph, validItems);
        }

        private static void AddIfValid(List<HrkPlayerInventory> list, HrkPlayerInventory? item, long? slotItemId, long heroId, long playerId)
        {
            if (item != null && slotItemId.HasValue && item.Id == slotItemId.Value
                && item.PlayerId == playerId && item.IsActive && item.IsEquipped && item.EquippedHeroId == heroId)
            {
                list.Add(item);
            }
        }

        public HeroStatCalculationResult CalculateStats(HrkPlayerHero? ph, IEnumerable<HrkPlayerInventory>? equippedItems)
        {
            var result = new HeroStatCalculationResult();
            if (ph == null)
            {
                result.Breakdowns = CreateEmptyBreakdowns();
                return result;
            }

            var ht = ph.HeroTemplate;
            var safeItems = (equippedItems ?? Enumerable.Empty<HrkPlayerInventory>())
                .Where(i => i != null && i.PlayerId == ph.PlayerId && i.IsActive && i.IsEquipped && i.EquippedHeroId == ph.Id)
                .ToList();

            // 1. Base Stats
            decimal baseHp = ht?.BaseHp ?? 0;
            decimal baseAtk = ht?.BaseAtk ?? 0;
            decimal baseDef = ht?.BaseDef ?? 0;
            decimal baseSpd = ht?.BaseSpd ?? 0;
            decimal baseCrit = ht?.BaseCrit ?? 0m;
            decimal baseCritDmg = ht?.BaseCritDmg ?? 0m;
            decimal baseLifesteal = ht?.BaseLifesteal ?? 0m;
            decimal baseAccuracy = ht?.BaseAccuracy ?? 0m;
            decimal baseResistance = ht?.BaseResistance ?? 0m;
            decimal baseMagicDamage = ht?.BaseMagicDamage ?? 0;
            decimal baseMagicResistance = ht?.BaseMagicResistance ?? 0;

            // 2. Growth / Override
            decimal growthHp = 0;
            decimal growthAtk = 0;
            decimal growthDef = 0;
            decimal growthSpd = 0;
            decimal growthCrit = 0;
            decimal growthCritDmg = 0;
            decimal growthLifesteal = 0;
            decimal growthAccuracy = 0;
            decimal growthResistance = 0;
            decimal growthMagicDamage = 0;
            decimal growthMagicResistance = 0;

            bool hasOverrideStats = false;
            if (!string.IsNullOrWhiteSpace(ph.CurrentStats))
            {
                try
                {
                    var parsed = JsonSerializer.Deserialize<CalculatedStatsDto>(ph.CurrentStats, CaseInsensitiveOptions);
                    if (parsed != null && (parsed.Hp > 0 || parsed.Atk > 0))
                    {
                        growthHp = Math.Max(0, parsed.Hp - baseHp);
                        growthAtk = Math.Max(0, parsed.Atk - baseAtk);
                        growthDef = Math.Max(0, parsed.Def - baseDef);
                        growthSpd = Math.Max(0, parsed.Spd - baseSpd);
                        growthCrit = Math.Max(0, parsed.Crit - baseCrit);
                        growthCritDmg = Math.Max(0, parsed.CritDmg - baseCritDmg);
                        growthLifesteal = Math.Max(0, parsed.Lifesteal - baseLifesteal);
                        growthAccuracy = Math.Max(0, parsed.Accuracy - baseAccuracy);
                        growthResistance = Math.Max(0, parsed.Resistance - baseResistance);

                        if (parsed.MagicDamage > 0 || baseMagicDamage > 0)
                        {
                            decimal effMagicDamage = parsed.MagicDamage;
                            if (effMagicDamage == 0 && baseMagicDamage > 0)
                            {
                                decimal lMult = 1.0m + Math.Max(0, ph.Level - 1) * 0.05m;
                                decimal sMult = 1.0m + Math.Max(0, ph.Stars - 1) * 0.15m;
                                effMagicDamage = Math.Round(baseMagicDamage * (lMult * sMult), MidpointRounding.AwayFromZero);
                            }
                            growthMagicDamage = Math.Max(0, effMagicDamage - baseMagicDamage);
                        }

                        if (parsed.MagicResistance > 0 || baseMagicResistance > 0)
                        {
                            decimal effMagicRes = parsed.MagicResistance;
                            if (effMagicRes == 0 && baseMagicResistance > 0)
                            {
                                decimal lMult = 1.0m + Math.Max(0, ph.Level - 1) * 0.05m;
                                decimal sMult = 1.0m + Math.Max(0, ph.Stars - 1) * 0.15m;
                                effMagicRes = Math.Round(baseMagicResistance * (lMult * sMult), MidpointRounding.AwayFromZero);
                            }
                            growthMagicResistance = Math.Max(0, effMagicRes - baseMagicResistance);
                        }

                        hasOverrideStats = true;
                    }
                }
                catch { }
            }

            if (!hasOverrideStats && ht != null)
            {
                decimal levelMultiplier = 1.0m + Math.Max(0, ph.Level - 1) * 0.05m;
                decimal starMultiplier = 1.0m + Math.Max(0, ph.Stars - 1) * 0.15m;
                decimal totalMultiplier = levelMultiplier * starMultiplier;

                growthHp = Math.Round(ht.BaseHp * (totalMultiplier - 1.0m), 2);
                growthAtk = Math.Round(ht.BaseAtk * (totalMultiplier - 1.0m), 2);
                growthDef = Math.Round(ht.BaseDef * (totalMultiplier - 1.0m), 2);
                growthSpd = Math.Round(ht.BaseSpd * (Math.Max(0, ph.Level - 1) * 0.01m), 2);
                growthMagicDamage = Math.Round(ht.BaseMagicDamage * (totalMultiplier - 1.0m), 2);
                growthMagicResistance = Math.Round(ht.BaseMagicResistance * (totalMultiplier - 1.0m), 2);
            }

            // 3. Equipment contributions
            var eqSources = new Dictionary<string, List<HeroStatSourceDto>>(StringComparer.OrdinalIgnoreCase)
            {
                ["HP"] = new(),
                ["ATK"] = new(),
                ["DEF"] = new(),
                ["SPD"] = new(),
                ["CRIT_RATE"] = new(),
                ["CRIT_DAMAGE"] = new(),
                ["LIFESTEAL"] = new(),
                ["ACCURACY"] = new(),
                ["RESISTANCE"] = new(),
                ["MAGIC_DAMAGE"] = new(),
                ["MAGIC_RESISTANCE"] = new()
            };

            foreach (var item in safeItems)
            {
                // Instance attributes are the source of truth for rolled equipment. CurrentStats is
                // only a denormalized/legacy cache and can be stale after rolling or enhancement.
                // Always calculate from the loaded instance first so formation/battle stats match
                // inventory and reward results.
                Dictionary<string, decimal>? itemStats = _itemStatCalculationService.CalculateCurrentStats(item);

                // Legacy fallback for old inventory rows that have neither instance nor template
                // attributes available, but still contain a valid CurrentStats snapshot.
                if ((itemStats == null || itemStats.Count == 0) && !string.IsNullOrWhiteSpace(item.CurrentStats))
                {
                    try
                    {
                        var parsedDict = JsonSerializer.Deserialize<Dictionary<string, decimal>>(item.CurrentStats, CaseInsensitiveOptions);
                        if (parsedDict != null && parsedDict.Count > 0)
                        {
                            itemStats = parsedDict;
                        }
                    }
                    catch { }
                }

                if (itemStats == null) continue;

                string itemName = item.ItemTemplate?.Name ?? $"Trang bị #{item.Id}";

                foreach (var kvp in itemStats)
                {
                    var targetStat = MapToTargetStat(kvp.Key);
                    if (targetStat == null || !eqSources.ContainsKey(targetStat)) continue;

                    decimal val = kvp.Value;
                    // Chuan hoa % neu gia tri o dang thap phan nhu 0.05 -> 5%
                    if (IsPercentageStat(targetStat))
                    {
                        if (val > 0 && val <= 1.0m)
                        {
                            val = Math.Round(val * 100m, 2);
                        }
                        else
                        {
                            val = Math.Round(val, 2);
                        }
                    }
                    else
                    {
                        val = Math.Round(val, 2);
                    }

                    if (val <= 0) continue;

                    eqSources[targetStat].Add(new HeroStatSourceDto
                    {
                        SourceType = "EQUIPMENT",
                        SourceName = itemName,
                        ItemName = itemName,
                        InventoryItemId = item.Id,
                        Value = val
                    });
                }
            }

            // 4. Build Stat Breakdowns
            var breakdowns = new List<HeroStatBreakdownDto>
            {
                BuildStatBreakdown("HP", "Máu (HP)", baseHp, growthHp, eqSources["HP"], isPercentage: false),
                BuildStatBreakdown("ATK", "Công (ATK)", baseAtk, growthAtk, eqSources["ATK"], isPercentage: false),
                BuildStatBreakdown("DEF", "Thủ (DEF)", baseDef, growthDef, eqSources["DEF"], isPercentage: false),
                BuildStatBreakdown("SPD", "Tốc độ (SPD)", baseSpd, growthSpd, eqSources["SPD"], isPercentage: false),
                BuildStatBreakdown("CRIT_RATE", "Bạo kích", baseCrit, growthCrit, eqSources["CRIT_RATE"], isPercentage: true),
                BuildStatBreakdown("CRIT_DAMAGE", "Sát thương bạo", baseCritDmg, growthCritDmg, eqSources["CRIT_DAMAGE"], isPercentage: true),
                BuildStatBreakdown("LIFESTEAL", "Hút máu", baseLifesteal, growthLifesteal, eqSources["LIFESTEAL"], isPercentage: true),
                BuildStatBreakdown("ACCURACY", "Chính xác", baseAccuracy, growthAccuracy, eqSources["ACCURACY"], isPercentage: true),
                BuildStatBreakdown("RESISTANCE", "Kháng hiệu ứng", baseResistance, growthResistance, eqSources["RESISTANCE"], isPercentage: true),
                BuildStatBreakdown("MAGIC_DAMAGE", "Sát thương phép", baseMagicDamage, growthMagicDamage, eqSources["MAGIC_DAMAGE"], isPercentage: false),
                BuildStatBreakdown("MAGIC_RESISTANCE", "Kháng phép", baseMagicResistance, growthMagicResistance, eqSources["MAGIC_RESISTANCE"], isPercentage: false)
            };

            result.Breakdowns = breakdowns;

            // 5. Populate Final Calculated Stats
            var hpBd = breakdowns.First(b => b.StatCode == "HP");
            var atkbd = breakdowns.First(b => b.StatCode == "ATK");
            var defBd = breakdowns.First(b => b.StatCode == "DEF");
            var spdBd = breakdowns.First(b => b.StatCode == "SPD");
            var critBd = breakdowns.First(b => b.StatCode == "CRIT_RATE");
            var critDmgBd = breakdowns.First(b => b.StatCode == "CRIT_DAMAGE");
            var lifestealBd = breakdowns.First(b => b.StatCode == "LIFESTEAL");
            var accBd = breakdowns.First(b => b.StatCode == "ACCURACY");
            var resBd = breakdowns.First(b => b.StatCode == "RESISTANCE");
            var magicDmgBd = breakdowns.First(b => b.StatCode == "MAGIC_DAMAGE");
            var magicResBd = breakdowns.First(b => b.StatCode == "MAGIC_RESISTANCE");

            result.FinalStats = new CalculatedStatsDto
            {
                Hp = (int)Math.Round(hpBd.Total, MidpointRounding.AwayFromZero),
                Atk = (int)Math.Round(atkbd.Total, MidpointRounding.AwayFromZero),
                Def = (int)Math.Round(defBd.Total, MidpointRounding.AwayFromZero),
                Spd = (int)Math.Round(spdBd.Total, MidpointRounding.AwayFromZero),
                Crit = Math.Round(critBd.Total, 2),
                CritDmg = Math.Round(critDmgBd.Total, 2),
                Lifesteal = Math.Round(lifestealBd.Total, 2),
                Accuracy = Math.Round(accBd.Total, 2),
                Resistance = Math.Round(resBd.Total, 2),
                MagicDamage = (int)Math.Round(magicDmgBd.Total, MidpointRounding.AwayFromZero),
                MagicResistance = (int)Math.Round(magicResBd.Total, MidpointRounding.AwayFromZero)
            };

            return result;
        }

        private static HeroStatBreakdownDto BuildStatBreakdown(
            string statCode,
            string displayName,
            decimal baseVal,
            decimal growthVal,
            List<HeroStatSourceDto> eqList,
            bool isPercentage)
        {
            decimal equipmentVal = eqList.Sum(s => s.Value);
            decimal auraVal = 0m;
            decimal otherVal = 0m;
            decimal total = baseVal + growthVal + equipmentVal + auraVal + otherVal;

            if (isPercentage)
            {
                baseVal = Math.Round(baseVal, 2);
                growthVal = Math.Round(growthVal, 2);
                equipmentVal = Math.Round(equipmentVal, 2);
                total = Math.Round(total, 2);
            }
            else
            {
                baseVal = Math.Round(baseVal, 0);
                growthVal = Math.Round(growthVal, 0);
                equipmentVal = Math.Round(equipmentVal, 0);
                total = Math.Round(total, 0);
            }

            var sources = new List<HeroStatSourceDto>();
            if (baseVal > 0)
            {
                sources.Add(new HeroStatSourceDto
                {
                    SourceType = "BASE",
                    SourceName = "Cơ bản",
                    Value = baseVal
                });
            }

            if (growthVal > 0)
            {
                sources.Add(new HeroStatSourceDto
                {
                    SourceType = "HERO_GROWTH",
                    SourceName = "Tăng trưởng",
                    Value = growthVal
                });
            }

            foreach (var s in eqList)
            {
                if (s.Value > 0) sources.Add(s);
            }

            if (auraVal > 0)
            {
                sources.Add(new HeroStatSourceDto
                {
                    SourceType = "AURA",
                    SourceName = "Hào quang",
                    Value = auraVal
                });
            }

            if (otherVal > 0)
            {
                sources.Add(new HeroStatSourceDto
                {
                    SourceType = "OTHER",
                    SourceName = "Khác",
                    Value = otherVal
                });
            }

            return new HeroStatBreakdownDto
            {
                StatCode = statCode,
                DisplayName = displayName,
                Total = total,
                BaseValue = baseVal,
                HeroGrowthValue = growthVal,
                EquipmentValue = equipmentVal,
                AuraValue = auraVal,
                OtherValue = otherVal,
                Sources = sources
            };
        }

        private static string? MapToTargetStat(string code)
        {
            var upper = (code ?? "").Trim().ToUpperInvariant();
            return upper switch
            {
                "HP" or "HEALTH" or "MAXHP" => "HP",
                "ATK" or "PHYSICAL_ATK" or "PHYSICALATK" or "ATTACK" => "ATK",
                "MAGIC_DAMAGE" or "MAGIC_ATK" or "MAGICATK" or "MATK" or "MDMG" => "MAGIC_DAMAGE",
                "DEF" or "ARMOR" or "DEFENSE" or "DEFENCE" => "DEF",
                "MAGIC_RESISTANCE" or "MAGIC_RESIST" or "MRES" or "MDEF" => "MAGIC_RESISTANCE",
                "SPD" or "SPEED" => "SPD",
                "CRIT" or "CRIT_RATE" or "CRITRATE" or "CRITICAL" => "CRIT_RATE",
                "CRITDMG" or "CRIT_DAMAGE" or "CRITDAMAGE" or "CRITICAL_DAMAGE" => "CRIT_DAMAGE",
                "LIFESTEAL" or "LIFE_STEAL" or "VAMP" => "LIFESTEAL",
                "ACCURACY" or "ACC" => "ACCURACY",
                "RESISTANCE" or "RES" or "CRIT_RESIST" or "EFFECT_RESIST" => "RESISTANCE",
                _ => null
            };
        }

        private static bool IsPercentageStat(string targetStat)
        {
            return targetStat is "CRIT_RATE" or "CRIT_DAMAGE" or "LIFESTEAL" or "ACCURACY" or "RESISTANCE";
        }

        private static List<HeroStatBreakdownDto> CreateEmptyBreakdowns()
        {
            return new List<HeroStatBreakdownDto>
            {
                new() { StatCode = "HP", DisplayName = "Máu (HP)" },
                new() { StatCode = "ATK", DisplayName = "Công (ATK)" },
                new() { StatCode = "DEF", DisplayName = "Thủ (DEF)" },
                new() { StatCode = "SPD", DisplayName = "Tốc độ (SPD)" },
                new() { StatCode = "CRIT_RATE", DisplayName = "Bạo kích" },
                new() { StatCode = "CRIT_DAMAGE", DisplayName = "Sát thương bạo" },
                new() { StatCode = "LIFESTEAL", DisplayName = "Hút máu" },
                new() { StatCode = "ACCURACY", DisplayName = "Chính xác" },
                new() { StatCode = "RESISTANCE", DisplayName = "Kháng hiệu ứng" },
                new() { StatCode = "MAGIC_DAMAGE", DisplayName = "Sát thương phép" },
                new() { StatCode = "MAGIC_RESISTANCE", DisplayName = "Kháng phép" }
            };
        }
    }
}
