using System.Collections.Generic;

namespace GAME.Application.DTOs
{
    public class PhaseDurationDto
    {
        public int Phase1Duration { get; set; }
        public int Phase2Duration { get; set; }
    }

    public class SkillTemplateDto
    {
        public string Id { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string Icon { get; set; } = null!;
        public string? Description { get; set; }
        public int Cost { get; set; }
        public string CostTypeCode { get; set; } = null!;
        public string CostTypeName { get; set; } = null!;
        public string CategoryCode { get; set; } = null!;
        public string CategoryName { get; set; } = null!;
        public string DamageTypeCode { get; set; } = null!;
        public string DamageTypeName { get; set; } = null!;
        public string EffectTypeCode { get; set; } = null!;
        public string EffectTypeName { get; set; } = null!;
        public bool IsDebuff { get; set; }
        public decimal DamageMultiplier { get; set; }
        public string TargetType { get; set; } = "single";
        public string Cooldown { get; set; } = "0s";
        public PhaseDurationDto? PhaseDurations { get; set; }
    }

    public class HeroTemplateDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string Avatar { get; set; } = null!;
        public int FactionId { get; set; }
        public string FactionCode { get; set; } = null!;
        public string FactionName { get; set; } = null!;
        public int ClassId { get; set; }
        public string ClassCode { get; set; } = null!;
        public string ClassName { get; set; } = null!;
        public int RarityId { get; set; }
        public string RarityCode { get; set; } = null!;
        public string RarityName { get; set; } = null!;
        public string? RarityColorHex { get; set; }
        public int BaseHp { get; set; }
        public int BaseAtk { get; set; }
        public int BaseDef { get; set; }
        public int BaseSpd { get; set; }
        public decimal BaseCrit { get; set; }
        public decimal BaseCritDmg { get; set; }
        public decimal BaseLifesteal { get; set; }
        public decimal BaseAccuracy { get; set; }
        public decimal BaseResistance { get; set; }
        public List<SkillTemplateDto> Skills { get; set; } = new();
    }

    public class ItemAttributeDto
    {
        public int AttributeTypeId { get; set; }
        public string AttributeCode { get; set; } = null!;
        public string AttributeName { get; set; } = null!;
        public bool IsPercentage { get; set; }
        public decimal Value { get; set; }
        public int DisplayOrder { get; set; }
    }

    public class AttributeTypeDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = null!;
        public string Name { get; set; } = null!;
        public bool IsPercentage { get; set; }
        public int DisplayOrder { get; set; }
        public string? Description { get; set; }
    }

    public class CategoryAllowedAttributeDto
    {
        public int Id { get; set; }
        public int CategoryId { get; set; }
        public int AttributeTypeId { get; set; }
        public string AttributeCode { get; set; } = null!;
        public string AttributeName { get; set; } = null!;
        public bool IsMainStat { get; set; }
        public bool IsSubStat { get; set; }
        public decimal? MinValue { get; set; }
        public decimal? MaxValue { get; set; }
        public int DisplayOrder { get; set; }
    }

    public class ItemTemplateDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = null!;
        public int CategoryId { get; set; }
        public string CategoryCode { get; set; } = null!;
        public string CategoryName { get; set; } = null!;
        public int RarityId { get; set; }
        public string RarityCode { get; set; } = null!;
        public string RarityName { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string? ImagePath { get; set; }
        public string? Icon { get; set; }
        public int LevelReq { get; set; }
        public string? Description { get; set; }
        public string? MetadataJson { get; set; }
        public object? BaseStats { get; set; }
        public List<ItemAttributeDto> Attributes { get; set; } = new();
        public bool IsStackable { get; set; }
        public int MaxStackSize { get; set; }
        public int SellPrice { get; set; }
    }
}
