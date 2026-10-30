using System.Collections.Generic;

namespace GAME.Application.DTOs
{

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
}
