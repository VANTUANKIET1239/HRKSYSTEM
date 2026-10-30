using System.Collections.Generic;

namespace GAME.Application.DTOs
{

    public class PlayerFormationSummaryDto
    {
        public int TemplateId { get; set; }
        public string Code { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public string? ImagePath { get; set; }
        public int Level { get; set; } = 1;
        public int MaxLevel { get; set; } = 5;
        public bool IsUnlocked { get; set; } = true;
        public bool IsSelected { get; set; } = false;
        public int BaseHeroPower { get; set; } = 0;
        public int FormationBonusPower { get; set; } = 0;
        public int TotalPower { get; set; } = 0;
        public int HeroCount { get; set; } = 0;
        public FormationStatBonusDto CurrentBonus { get; set; } = new();
        public int DisplayOrder { get; set; }
    }
}
