using System.Collections.Generic;

namespace GAME.Application.DTOs
{

    public class FormationUpgradeCostDto
    {
        public long GoldCost { get; set; }
        public int StoneCost { get; set; }
        public int StoneItemTemplateId { get; set; }
        public long PlayerGold { get; set; }
        public int PlayerStones { get; set; }
        public bool CanUpgrade { get; set; }
        public string? CannotUpgradeReason { get; set; }
    }
}
