using System;

namespace GAME.Domain.Entities
{
    public class HrkSkillEffectScaling
    {
        public long Id { get; set; }
        public long SkillEffectId { get; set; }
        public int AttributeTypeId { get; set; }
        public decimal Coefficient { get; set; }
        public decimal FlatValue { get; set; }
        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedOn { get; set; } = DateTime.UtcNow;

        public virtual HrkSkillEffect SkillEffect { get; set; } = null!;
        public virtual HrkAttributeType AttributeType { get; set; } = null!;
    }
}
