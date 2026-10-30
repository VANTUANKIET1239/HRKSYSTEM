using System.Collections.Generic;

namespace GAME.Application.DTOs
{

    public class SkillTemplateDto
    {
        public string Id { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string? ImagePath { get; set; }
        public string Icon { get; set; } = null!;
        public string? Description { get; set; }
        public string SkillTypeCode { get; set; } = "ENERGY";
        public string TriggerCode { get; set; } = "MANUAL_ENERGY_FULL";
        public int EnergyCost { get; set; } = 100;
        public int DisplayOrder { get; set; }
        public List<SkillEffectDto> Effects { get; set; } = new();
        public SkillAnimationConfigDto? Animation { get; set; }

        // Legacy compatibility properties
        public int Cost { get; set; }
        public string CostTypeCode { get; set; } = "MP";
        public string CostTypeName { get; set; } = "Năng Lượng";
        public string CategoryCode { get; set; } = "";
        public string CategoryName { get; set; } = "";
        public string DamageTypeCode { get; set; } = "";
        public string DamageTypeName { get; set; } = "";
        public string EffectTypeCode { get; set; } = "";
        public string EffectTypeName { get; set; } = "";
        public bool IsDebuff { get; set; }
        public decimal DamageMultiplier { get; set; }
        public string TargetType { get; set; } = "single";
        public string Cooldown { get; set; } = "0s";
        public PhaseDurationDto? PhaseDurations { get; set; }
    }
}
