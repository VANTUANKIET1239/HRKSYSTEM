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
        public CalculatedStatsDto Stats { get; set; } = new();
        public List<SkillTemplateDto> Skills { get; set; } = new();
    }

    public class PlayerHeroDetailDto : PlayerHeroDto
    {
        public HeroEquipmentDto Equipment { get; set; } = new();
    }
}
