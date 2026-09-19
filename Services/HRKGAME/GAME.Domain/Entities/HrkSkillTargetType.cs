using System;
using System.Collections.Generic;

namespace GAME.Domain.Entities
{
    public class HrkSkillTargetType
    {
        public int Id { get; set; }
        public string Code { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string TargetSide { get; set; } = null!;
        public string SelectionRule { get; set; } = null!;
        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedOn { get; set; } = DateTime.UtcNow;

        public virtual ICollection<HrkSkillEffect> Effects { get; set; } = new List<HrkSkillEffect>();
    }
}
