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
        public string? ImagePath { get; set; }
        public string SkillTypeCode { get; set; } = "ENERGY";
        public string TriggerCode { get; set; } = "MANUAL_ENERGY_FULL";
        public int EnergyCost { get; set; } = 100;
        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedOn { get; set; } = DateTime.UtcNow;

        public virtual ICollection<HrkHeroSkill> HeroSkills { get; set; } = new List<HrkHeroSkill>();
        public virtual ICollection<HrkSkillEffect> Effects { get; set; } = new List<HrkSkillEffect>();
    }
}
