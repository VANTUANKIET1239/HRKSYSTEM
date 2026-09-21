using System.Collections.Generic;

namespace GAME.Application.DTOs
{
    public class CalculatedStatsDto
    {
        public int Hp { get; set; }
        public int Atk { get; set; }
        public int Def { get; set; }
        public int Spd { get; set; }
        public decimal Crit { get; set; }
        public decimal CritDmg { get; set; }
        public decimal Lifesteal { get; set; }
        public decimal Accuracy { get; set; }
        public decimal Resistance { get; set; }
        public int MagicDamage { get; set; }
        public int MagicResistance { get; set; }
    }

    public class HeroStatSourceDto
    {
        public string SourceType { get; set; } = null!; // BASE, HERO_GROWTH, EQUIPMENT, AURA, OTHER
        public string SourceName { get; set; } = null!;
        public string? ItemName { get; set; }
        public long? InventoryItemId { get; set; }
        public decimal Value { get; set; }
    }

    public class HeroStatBreakdownDto
    {
        public string StatCode { get; set; } = null!;
        public string DisplayName { get; set; } = null!;
        public decimal Total { get; set; }

        public decimal BaseValue { get; set; }
        public decimal HeroGrowthValue { get; set; }
        public decimal EquipmentValue { get; set; }
        public decimal AuraValue { get; set; }
        public decimal OtherValue { get; set; }

        public List<HeroStatSourceDto> Sources { get; set; } = new();
    }

    public class PlayerHeroDto
    {
        public long Id { get; set; }
        public int HeroTemplateId { get; set; }
        public string Name { get; set; } = null!;
        public string Avatar { get; set; } = null!;
        public string FactionCode { get; set; } = null!;
        public string FactionName { get; set; } = null!;
        public string ClassCode { get; set; } = null!;
        public string ClassName { get; set; } = null!;
        public int RarityId { get; set; }
        public string RarityCode { get; set; } = null!;
        public string RarityName { get; set; } = null!;
        public string? RarityColorHex { get; set; }
        public int Level { get; set; }
        public int Exp { get; set; }
        public int MaxExp { get; set; }
        public int Stars { get; set; }
        public int Power { get; set; }
        public byte AuraTier { get; set; }
        public bool IsLocked { get; set; }
        public bool IsFavorite { get; set; }
        public int? Position { get; set; }
        public CalculatedStatsDto Stats { get; set; } = new();
        public List<SkillTemplateDto> Skills { get; set; } = new();
        public HeroStarAuraConfigDto? StarAura { get; set; }
    }

    public class HeroStarAuraConfigDto
    {
        public int HeroTemplateId { get; set; }
        public byte StarLevel { get; set; }
        public string AuraCode { get; set; } = null!;
        public string VisualKey { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public string? PrimaryColorHex { get; set; }
        public string? SecondaryColorHex { get; set; }
        public decimal Intensity { get; set; }
        public byte ParticleLevel { get; set; }
    }

    public class PlayerHeroDetailDto : PlayerHeroDto
    {
        public HeroEquipmentDto Equipment { get; set; } = new();
        public List<HeroStatBreakdownDto> StatBreakdowns { get; set; } = new();
    }

    public class EquipHeroItemRequestDto
    {
        public long InventoryItemId { get; set; }
    }

    public class HeroUpgradePreviewDto
    {
        public long HeroId { get; set; }
        public int CurrentLevel { get; set; }
        public int NextLevel { get; set; }
        public int MaxLevel { get; set; }
        public long GoldCost { get; set; }
        public int MaterialCost { get; set; }
        public CalculatedStatsDto CurrentStats { get; set; } = new();
        public CalculatedStatsDto NextStats { get; set; } = new();
        public CalculatedStatsDto StatIncrease { get; set; } = new();
    }

    public class UpgradeHeroRequestDto
    {
        /// <summary>Number of levels to buy. The server caps this at 50.</summary>
        public int Levels { get; set; } = 1;
    }
}
