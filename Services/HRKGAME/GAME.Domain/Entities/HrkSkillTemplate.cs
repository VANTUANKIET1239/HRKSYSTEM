using System;
using System.Collections.Generic;

namespace GAME.Domain.Entities
{
    public class HrkSkillTemplate
    {
        public string Id { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string Icon { get; set; } = null!;
        public string? Description { get; set; }
        public int Cost { get; set; }
        public int CostTypeId { get; set; }
        public int CategoryId { get; set; }
        public int DamageTypeId { get; set; }
        public int EffectTypeId { get; set; }
        public decimal DamageMultiplier { get; set; } = 1.0m;
        public string TargetType { get; set; } = "single";
        public string Cooldown { get; set; } = "0s";
        public string? PhaseDurations { get; set; }
        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;

        public virtual HrkSkillCostType CostType { get; set; } = null!;
        public virtual HrkSkillCategory Category { get; set; } = null!;
        public virtual HrkSkillDamageType DamageType { get; set; } = null!;
        public virtual HrkSkillEffectType EffectType { get; set; } = null!;

        public virtual ICollection<HrkHeroSkill> HeroSkills { get; set; } = new List<HrkHeroSkill>();
    }
}
