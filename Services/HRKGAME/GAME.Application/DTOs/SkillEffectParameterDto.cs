using System.Collections.Generic;

namespace GAME.Application.DTOs
{

    public class SkillEffectParameterDto
    {
        public string ParameterCode { get; set; } = null!;
        public decimal? DecimalValue { get; set; }
        public int? IntValue { get; set; }
        public bool? BoolValue { get; set; }
        public string? StringValue { get; set; }
    }
}
