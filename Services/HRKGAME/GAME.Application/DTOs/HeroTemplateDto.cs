using System.Collections.Generic;

namespace GAME.Application.DTOs
{

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
        public int BaseMagicDamage { get; set; }
        public int BaseMagicResistance { get; set; }
        public List<SkillTemplateDto> Skills { get; set; } = new();
    }
}
