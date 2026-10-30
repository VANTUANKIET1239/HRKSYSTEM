using System.Collections.Generic;

namespace GAME.Application.DTOs
{

    public class SkillEffectScalingDto
    {
        public string AttributeTypeCode { get; set; } = null!;
        public string AttributeTypeName { get; set; } = null!;
        public decimal Coefficient { get; set; }
        public decimal FlatValue { get; set; }
    }
}
