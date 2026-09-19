using System;
using System.Collections.Generic;

namespace GAME.Domain.Entities
{
    public class HrkSkillEffectType
    {
        public int Id { get; set; }
        public string Code { get; set; } = null!;
        public string Name { get; set; } = null!;
        public bool IsDebuff { get; set; }
        public string EffectGroup { get; set; } = "SPECIAL";
        public bool IsBeneficial { get; set; }
        public bool IsStackable { get; set; }
        public int? DefaultStackLimit { get; set; }
        public string? Description { get; set; }
        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedOn { get; set; } = DateTime.UtcNow;

        public virtual ICollection<HrkSkillEffect> Effects { get; set; } = new List<HrkSkillEffect>();
    }
}
