using System.Collections.Generic;

namespace GAME.Application.DTOs
{

    public class FormationPreviewResponseDto
    {
        public string FormationCode { get; set; } = null!;
        public string FormationName { get; set; } = null!;
        public int FormationLevel { get; set; }
        public FormationStatBonusDto CurrentBonus { get; set; } = new();
        public List<FormationSlotDto> Slots { get; set; } = new();
        public int BaseHeroPower { get; set; }
        public int FormationBonusPower { get; set; }
        public int TotalPower { get; set; }
        public bool IsValid { get; set; } = true;
        public List<string> ValidationErrors { get; set; } = new();
    }
}
