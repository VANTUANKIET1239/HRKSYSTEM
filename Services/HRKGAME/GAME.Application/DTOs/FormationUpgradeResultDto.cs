using System.Collections.Generic;

namespace GAME.Application.DTOs
{

    public class FormationUpgradeResultDto
    {
        public string Code { get; set; } = null!;
        public int NewLevel { get; set; }
        public FormationStatBonusDto NewBonus { get; set; } = new();
        public long NewWalletGold { get; set; }
        public int NewStoneQuantity { get; set; }
        public int BaseHeroPower { get; set; }
        public int FormationBonusPower { get; set; }
        public int TotalPower { get; set; }
        public FormationUpgradeCostDto? NextUpgradeCost { get; set; }
    }
}
