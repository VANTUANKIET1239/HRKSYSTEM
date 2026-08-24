using System;
using System.Collections.Generic;

namespace GAME.Domain.Entities
{
    public class HrkSkillDamageType
    {
        public int Id { get; set; }
        public string Code { get; set; } = null!;
        public string Name { get; set; } = null!;
        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;

        public virtual ICollection<HrkSkillTemplate> SkillTemplates { get; set; } = new List<HrkSkillTemplate>();
    }
}
