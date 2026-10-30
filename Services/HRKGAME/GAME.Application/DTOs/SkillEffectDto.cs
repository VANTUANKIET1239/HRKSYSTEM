using System.Collections.Generic;

namespace GAME.Application.DTOs
{

    public class SkillEffectDto
    {
        public long Id { get; set; }
        public string EffectTypeCode { get; set; } = null!;
        public string EffectTypeName { get; set; } = null!;
        public string EffectGroup { get; set; } = "SPECIAL";
        public bool IsBeneficial { get; set; }
        public string? EffectDescription { get; set; }
        public string? EffectImagePath { get; set; }
        public string? EffectColorHex { get; set; }
        public string TargetTypeCode { get; set; } = null!;
        public string TargetTypeName { get; set; } = null!;
        public string TargetSide { get; set; } = "ENEMY";
        public string SelectionRule { get; set; } = "SINGLE";
        public string? DamageSchoolCode { get; set; }
        public decimal BaseValue { get; set; }
        public int? DurationTurns { get; set; }
        public decimal ChancePercent { get; set; } = 100m;
        public int? MaxStacks { get; set; }
        public int DisplayOrder { get; set; }
        public string? ExecutionGroup { get; set; }
        public string? ConditionCode { get; set; }
        public List<SkillEffectScalingDto> Scalings { get; set; } = new();
        public List<SkillEffectStatModifierDto> StatModifiers { get; set; } = new();
        public List<SkillEffectParameterDto> Parameters { get; set; } = new();
    }
}
