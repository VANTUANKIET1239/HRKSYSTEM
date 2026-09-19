using GAME.Application.Common.Helpers;
using GAME.Application.DTOs;
using GAME.Domain.Entities;
using System.Collections.Generic;
using System.Linq;

namespace GAME.Application.Common.Mappings
{
    public static class GameDtoMapper
    {
        /// <summary>
        /// Map HrkPlayerInventory entity sang InventoryItemDto.
        /// </summary>
        public static InventoryItemDto? MapInventoryItem(HrkPlayerInventory? inv)
        {
            if (inv == null || inv.ItemTemplate == null) return null;
            var it = inv.ItemTemplate;
            return new InventoryItemDto
            {
                Id = inv.Id,
                ItemTemplateId = inv.ItemTemplateId,
                ItemCode = it.Code ?? "",
                Name = it.Name,
                ImagePath = it.ImagePath,
                Icon = it.ImagePath ?? it.Icon ?? "",
                RarityId = it.RarityId,
                RarityCode = it.Rarity?.Code ?? "",
                RarityName = it.Rarity?.Name ?? "",
                RarityColorHex = it.Rarity?.ColorHex,
                CategoryId = it.CategoryId,
                CategoryCode = it.Category?.Code ?? "",
                CategoryName = it.Category?.Name ?? "",
                IsEquipment = it.Category?.IsEquipment ?? false,
                Count = inv.Count,
                LevelReq = it.LevelReq,
                Description = it.Description,
                Stats = MapItemStatsObject(inv.CurrentStats, it),
                Attributes = it.Attributes?.Select(MapItemAttribute).ToList(),
                IsLocked = inv.IsLocked,
                IsEquipped = inv.IsEquipped,
                EquippedHeroId = inv.EquippedHeroId,
                Enhancement = inv.Enhancement,
                Stars = inv.Stars,
                SlotIndex = inv.SlotIndex
            };
        }

        /// <summary>
        /// Map HrkItemTemplateAttribute entity sang ItemAttributeDto.
        /// </summary>
        public static ItemAttributeDto MapItemAttribute(HrkItemTemplateAttribute attr)
        {
            return new ItemAttributeDto
            {
                AttributeTypeId = attr.AttributeTypeId,
                AttributeCode = attr.AttributeType?.Code ?? "",
                AttributeName = attr.AttributeType?.Name ?? "",
                IsPercentage = attr.AttributeType?.IsPercentage ?? false,
                Value = attr.Value,
                DisplayOrder = attr.AttributeType?.DisplayOrder ?? 0
            };
        }

        /// <summary>
        /// Tạo đối tượng stats đa năng từ CurrentStats JSON (hoặc thuộc tính gốc),
        /// bổ sung alias chữ thường (atk, def, hp, crit, spd) tương thích UI.
        /// </summary>
        private static object? MapItemStatsObject(string? currentStatsJson, HrkItemTemplate it)
        {
            var dict = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

            if (!string.IsNullOrWhiteSpace(currentStatsJson))
            {
                try
                {
                    var parsed = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(currentStatsJson);
                    if (parsed != null)
                    {
                        foreach (var kvp in parsed)
                        {
                            dict[kvp.Key] = kvp.Value;
                        }
                    }
                }
                catch { }
            }
            else if (it.Attributes != null && it.Attributes.Count > 0)
            {
                foreach (var attr in it.Attributes)
                {
                    var code = attr.AttributeType?.Code ?? $"ATTR_{attr.AttributeTypeId}";
                    dict[code] = attr.Value;
                }
            }

            // Bổ sung alias tương thích với giao diện Angular (atk, def, hp, crit, spd, magicDamage, magicResistance)
            if (dict.TryGetValue("PHYSICAL_ATK", out var pAtk) && !dict.ContainsKey("atk")) dict["atk"] = pAtk;
            if (dict.TryGetValue("MAGIC_DAMAGE", out var mDmg) && !dict.ContainsKey("magicDamage")) dict["magicDamage"] = mDmg;
            else if (dict.TryGetValue("MAGIC_ATK", out var mAtk) && !dict.ContainsKey("magicDamage")) dict["magicDamage"] = mAtk;
            else if (dict.TryGetValue("MATK", out var mAtkShort) && !dict.ContainsKey("magicDamage")) dict["magicDamage"] = mAtkShort;

            if (dict.TryGetValue("MAGIC_RESISTANCE", out var mRes) && !dict.ContainsKey("magicResistance")) dict["magicResistance"] = mRes;
            else if (dict.TryGetValue("MAGIC_RESIST", out var mResist) && !dict.ContainsKey("magicResistance")) dict["magicResistance"] = mResist;
            else if (dict.TryGetValue("MRES", out var mResShort) && !dict.ContainsKey("magicResistance")) dict["magicResistance"] = mResShort;

            if (dict.TryGetValue("ARMOR", out var armor) && !dict.ContainsKey("def")) dict["def"] = armor;
            if (dict.TryGetValue("HP", out var hp) && !dict.ContainsKey("hp")) dict["hp"] = hp;
            if (dict.TryGetValue("SPEED", out var spd) && !dict.ContainsKey("spd")) dict["spd"] = spd;
            if (dict.TryGetValue("CRIT_RATE", out var crit) && !dict.ContainsKey("crit"))
            {
                if (decimal.TryParse(crit?.ToString(), out var critVal))
                {
                    dict["crit"] = critVal <= 1.0m ? critVal * 100 : critVal;
                }
                else
                {
                    dict["crit"] = crit;
                }
            }
            if (dict.TryGetValue("CRIT_DAMAGE", out var critDmg) && !dict.ContainsKey("critDmg"))
            {
                if (decimal.TryParse(critDmg?.ToString(), out var critDmgVal))
                {
                    dict["critDmg"] = critDmgVal <= 2.0m ? critDmgVal * 100 : critDmgVal;
                }
                else
                {
                    dict["critDmg"] = critDmg;
                }
            }

            return dict.Count > 0 ? dict : null;
        }

        /// <summary>
        /// Map HrkPlayerEquipment entity sang HeroEquipmentDto.
        /// </summary>
        public static HeroEquipmentDto MapHeroEquipment(HrkPlayerEquipment? eq, long heroId)
        {
            return new HeroEquipmentDto
            {
                HeroId = heroId,
                Weapon = MapInventoryItem(eq?.Weapon),
                Armor = MapInventoryItem(eq?.Armor),
                Helmet = MapInventoryItem(eq?.Helmet),
                Boots = MapInventoryItem(eq?.Boots),
                Ring = MapInventoryItem(eq?.Ring),
                Artifact = MapInventoryItem(eq?.Artifact)
            };
        }

        /// <summary>
        /// Map HrkSkillEffectScaling sang SkillEffectScalingDto.
        /// </summary>
        public static SkillEffectScalingDto MapSkillEffectScaling(HrkSkillEffectScaling sc)
        {
            return new SkillEffectScalingDto
            {
                AttributeTypeCode = sc.AttributeType?.Code ?? "",
                AttributeTypeName = sc.AttributeType?.Name ?? "",
                Coefficient = sc.Coefficient,
                FlatValue = sc.FlatValue
            };
        }

        /// <summary>
        /// Map HrkSkillEffectStatModifier sang SkillEffectStatModifierDto.
        /// </summary>
        public static SkillEffectStatModifierDto MapSkillEffectStatModifier(HrkSkillEffectStatModifier sm)
        {
            return new SkillEffectStatModifierDto
            {
                AttributeTypeCode = sm.AttributeType?.Code ?? "",
                AttributeTypeName = sm.AttributeType?.Name ?? "",
                ValueType = sm.ValueType,
                Value = sm.Value
            };
        }

        /// <summary>
        /// Map HrkSkillEffect sang SkillEffectDto.
        /// </summary>
        public static SkillEffectDto MapSkillEffect(HrkSkillEffect e)
        {
            return new SkillEffectDto
            {
                Id = e.Id,
                EffectTypeCode = e.EffectType?.Code ?? "",
                EffectTypeName = e.EffectType?.Name ?? "",
                EffectGroup = e.EffectType?.EffectGroup ?? "SPECIAL",
                IsBeneficial = e.EffectType?.IsBeneficial ?? false,
                TargetTypeCode = e.TargetType?.Code ?? "",
                TargetTypeName = e.TargetType?.Name ?? "",
                TargetSide = e.TargetType?.TargetSide ?? "ENEMY",
                SelectionRule = e.TargetType?.SelectionRule ?? "SINGLE",
                DamageSchoolCode = e.DamageSchoolCode,
                BaseValue = e.BaseValue,
                DurationTurns = e.DurationTurns,
                ChancePercent = e.ChancePercent,
                MaxStacks = e.MaxStacks,
                DisplayOrder = e.DisplayOrder,
                Scalings = e.Scalings?.Select(MapSkillEffectScaling).ToList() ?? new List<SkillEffectScalingDto>(),
                StatModifiers = e.StatModifiers?.Select(MapSkillEffectStatModifier).ToList() ?? new List<SkillEffectStatModifierDto>()
            };
        }

        /// <summary>
        /// Map HrkSkillTemplate entity sang SkillTemplateDto.
        /// </summary>
        public static SkillTemplateDto? MapSkillTemplate(HrkSkillTemplate? s)
        {
            if (s == null) return null;
            var primaryEffect = s.Effects?.Where(e => e.IsActive).OrderBy(e => e.DisplayOrder).FirstOrDefault();
            return new SkillTemplateDto
            {
                Id = s.Id,
                Name = s.Name,
                ImagePath = s.ImagePath,
                Icon = s.Icon,
                Description = s.Description,
                SkillTypeCode = s.SkillTypeCode,
                TriggerCode = s.TriggerCode,
                EnergyCost = s.EnergyCost,
                DisplayOrder = s.DisplayOrder,
                Effects = s.Effects?.Where(e => e.IsActive).OrderBy(e => e.DisplayOrder).Select(MapSkillEffect).ToList() ?? new List<SkillEffectDto>(),
                // Legacy compatibility synthesized from data-driven schema
                Cost = s.EnergyCost,
                CostTypeCode = s.EnergyCost > 0 ? "MP" : "NONE",
                CostTypeName = s.EnergyCost > 0 ? "Năng Lượng" : "Không tốn",
                CategoryCode = s.SkillTypeCode.ToLowerInvariant(),
                CategoryName = s.SkillTypeCode,
                DamageTypeCode = primaryEffect?.DamageSchoolCode ?? "",
                DamageTypeName = primaryEffect?.DamageSchoolCode ?? "",
                EffectTypeCode = primaryEffect?.EffectType?.Code ?? "",
                EffectTypeName = primaryEffect?.EffectType?.Name ?? "",
                IsDebuff = primaryEffect?.EffectType != null && !primaryEffect.EffectType.IsBeneficial,
                DamageMultiplier = 1.0m,
                TargetType = primaryEffect?.TargetType?.SelectionRule ?? "single",
                Cooldown = "0s",
                PhaseDurations = null
            };
        }

        /// <summary>
        /// Map HrkPlayerHero entity sang PlayerHeroDto.
        /// </summary>
        public static PlayerHeroDto? MapPlayerHero(HrkPlayerHero? ph)
        {
            if (ph == null) return null;
            var ht = ph.HeroTemplate;
            return new PlayerHeroDto
            {
                Id = ph.Id,
                HeroTemplateId = ph.HeroTemplateId,
                Name = ht?.Name ?? "",
                Avatar = ht?.Avatar ?? "",
                FactionCode = ht?.Faction?.Code ?? "",
                FactionName = ht?.Faction?.Name ?? "",
                ClassCode = ht?.Class?.Code ?? "",
                ClassName = ht?.Class?.Name ?? "",
                RarityId = ht?.RarityId ?? 0,
                RarityCode = ht?.Rarity?.Code ?? "",
                RarityName = ht?.Rarity?.Name ?? "",
                RarityColorHex = ht?.Rarity?.ColorHex,
                Level = ph.Level,
                Exp = ph.Exp,
                MaxExp = ph.MaxExp,
                Stars = ph.Stars,
                Power = ph.Power,
                AuraTier = ph.AuraTier,
                IsLocked = ph.IsLocked,
                IsFavorite = ph.IsFavorite,
                Stats = HeroStatsHelper.CalculateStats(ph),
                Skills = ht?.HeroSkills?.OrderBy(hs => hs.SkillOrder)
                    .Select(hs => MapSkillTemplate(hs.Skill)!)
                    .Where(s => s != null)
                    .ToList() ?? new List<SkillTemplateDto>()
            };
        }

        /// <summary>
        /// Map HrkHeroTemplate entity sang HeroTemplateDto.
        /// </summary>
        public static HeroTemplateDto? MapHeroTemplate(HrkHeroTemplate? h)
        {
            if (h == null) return null;
            return new HeroTemplateDto
            {
                Id = h.Id,
                Name = h.Name,
                Avatar = h.Avatar,
                FactionId = h.FactionId,
                FactionCode = h.Faction?.Code ?? "",
                FactionName = h.Faction?.Name ?? "",
                ClassId = h.ClassId,
                ClassCode = h.Class?.Code ?? "",
                ClassName = h.Class?.Name ?? "",
                RarityId = h.RarityId,
                RarityCode = h.Rarity?.Code ?? "",
                RarityName = h.Rarity?.Name ?? "",
                RarityColorHex = h.Rarity?.ColorHex,
                BaseHp = h.BaseHp,
                BaseAtk = h.BaseAtk,
                BaseDef = h.BaseDef,
                BaseSpd = h.BaseSpd,
                BaseCrit = h.BaseCrit,
                BaseCritDmg = h.BaseCritDmg,
                BaseLifesteal = h.BaseLifesteal,
                BaseAccuracy = h.BaseAccuracy,
                BaseResistance = h.BaseResistance,
                BaseMagicDamage = h.BaseMagicDamage,
                BaseMagicResistance = h.BaseMagicResistance,
                Skills = h.HeroSkills?.OrderBy(hs => hs.SkillOrder)
                    .Select(hs => MapSkillTemplate(hs.Skill)!)
                    .Where(s => s != null)
                    .ToList() ?? new List<SkillTemplateDto>()
            };
        }
    }
}
