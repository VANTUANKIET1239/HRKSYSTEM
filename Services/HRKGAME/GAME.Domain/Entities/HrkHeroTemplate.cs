using System;
using System.Collections.Generic;

namespace GAME.Domain.Entities
{
    public class HrkHeroTemplate
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string Avatar { get; set; } = null!;
        public int FactionId { get; set; }
        public int ClassId { get; set; }
        public int RarityId { get; set; }
        public int BaseHp { get; set; } = 1000;
        public int BaseAtk { get; set; } = 150;
        public int BaseDef { get; set; } = 80;
        public int BaseSpd { get; set; } = 100;
        public decimal BaseCrit { get; set; } = 5.0m;
        public decimal BaseCritDmg { get; set; } = 150.0m;
        public decimal BaseLifesteal { get; set; } = 0.0m;
        public decimal BaseAccuracy { get; set; } = 80.0m;
        public decimal BaseResistance { get; set; } = 10.0m;
        public int BaseMagicDamage { get; set; }
        public int BaseMagicResistance { get; set; }
        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;

        public virtual HrkHeroFaction Faction { get; set; } = null!;
        public virtual HrkHeroClass Class { get; set; } = null!;
        public virtual HrkRarity Rarity { get; set; } = null!;

        public virtual ICollection<HrkHeroSkill> HeroSkills { get; set; } = new List<HrkHeroSkill>();
        public virtual ICollection<HrkPlayerHero> PlayerHeroes { get; set; } = new List<HrkPlayerHero>();
        public virtual ICollection<HrkHeroStarAuraConfig> StarAuraConfigs { get; set; } = new List<HrkHeroStarAuraConfig>();
    }
}
