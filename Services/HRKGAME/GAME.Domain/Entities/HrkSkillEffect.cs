using System;
using System.Collections.Generic;

namespace GAME.Domain.Entities
{
    public class HrkSkillEffect
    {
        public long Id { get; set; }
        public string SkillId { get; set; } = null!;
        public int EffectTypeId { get; set; }
        public int TargetTypeId { get; set; }
        public string? DamageSchoolCode { get; set; }
        public decimal BaseValue { get; set; }
        public int? DurationTurns { get; set; }
        public decimal ChancePercent { get; set; } = 100.0m;
        public int? MaxStacks { get; set; }
        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedOn { get; set; } = DateTime.UtcNow;

        public virtual HrkSkillTemplate Skill { get; set; } = null!;
        public virtual HrkSkillEffectType EffectType { get; set; } = null!;
        public virtual HrkSkillTargetType TargetType { get; set; } = null!;

        public virtual ICollection<HrkSkillEffectScaling> Scalings { get; set; } = new List<HrkSkillEffectScaling>();
        public virtual ICollection<HrkSkillEffectStatModifier> StatModifiers { get; set; } = new List<HrkSkillEffectStatModifier>();
    }
}
