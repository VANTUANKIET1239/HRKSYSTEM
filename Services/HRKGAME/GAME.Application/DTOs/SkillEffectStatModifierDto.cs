using System.Collections.Generic;

namespace GAME.Application.DTOs
{

    public class SkillEffectStatModifierDto
    {
        public string AttributeTypeCode { get; set; } = null!;
        public string AttributeTypeName { get; set; } = null!;
        public string ValueType { get; set; } = "PERCENT";
        public decimal Value { get; set; }
    }
}
