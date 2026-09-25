using System;

namespace GAME.Domain.Entities
{
    public class HrkSkillEffectParameter
    {
        public long Id { get; set; }
        public long SkillEffectId { get; set; }
        public string ParameterCode { get; set; } = null!;
        public decimal? DecimalValue { get; set; }
        public int? IntValue { get; set; }
        public bool? BoolValue { get; set; }
        public string? StringValue { get; set; }
        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedOn { get; set; } = DateTime.UtcNow;

        public virtual HrkSkillEffect SkillEffect { get; set; } = null!;
    }
}
