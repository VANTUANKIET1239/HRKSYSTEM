using System.Collections.Generic;

namespace GAME.Application.DTOs
{
    public class FormationPositionDto
    {
        public int Slot { get; set; }
        public PlayerHeroDto? Hero { get; set; }
    }

    public class FormationDto
    {
        public long PlayerId { get; set; }
        public string FormationName { get; set; } = "Main Team";
        public int TotalPower { get; set; }
        public List<FormationPositionDto> Positions { get; set; } = new();
    }

    public class FormationStatBonusDto
    {
        public decimal HpPercent { get; set; } = 0m;
        public decimal AtkPercent { get; set; } = 0m;
        public decimal DefPercent { get; set; } = 0m;
        public decimal SpdPercent { get; set; } = 0m;
        public decimal MagicDamagePercent { get; set; } = 0m;
        public decimal MagicResistancePercent { get; set; } = 0m;
    }

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

    public class FormationSlotDto
    {
        public int Slot { get; set; }
        public string RowType { get; set; } = "FRONT"; // FRONT, BACK
        public int Lane { get; set; }
        public int DisplayX { get; set; }
        public int DisplayY { get; set; }
        public PlayerHeroDto? Hero { get; set; }
    }

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

    public class FormationDetailDto
    {
        public int TemplateId { get; set; }
        public string Code { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public string? ImagePath { get; set; }
        public int Level { get; set; } = 1;
        public int MaxLevel { get; set; } = 5;
        public bool IsSelected { get; set; } = false;
        public bool IsUnlocked { get; set; } = true;
        public List<FormationSlotDto> Slots { get; set; } = new();
        public int BaseHeroPower { get; set; } = 0;
        public int FormationBonusPower { get; set; } = 0;
        public int TotalPower { get; set; } = 0;
        public FormationStatBonusDto CurrentBonus { get; set; } = new();
        public FormationStatBonusDto? NextLevelBonus { get; set; }
        public FormationUpgradeCostDto? UpgradeCost { get; set; }
    }

    public class SlotHeroPositionDto
    {
        public int Slot { get; set; }
        public long? HeroId { get; set; }
    }

    public class UpdateFormationPositionsRequest
    {
        public List<SlotHeroPositionDto> Positions { get; set; } = new();
    }

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

    public class FormationPowerCalculationResult
    {
        public int BaseHeroPower { get; set; }
        public int FormationBonusPower { get; set; }
        public int TotalPower { get; set; }
        public Dictionary<long, int> HeroBasePowers { get; set; } = new();
        public Dictionary<long, int> HeroTotalPowers { get; set; } = new();
        public Dictionary<long, CalculatedStatsDto> HeroBaseStats { get; set; } = new();
        public Dictionary<long, CalculatedStatsDto> HeroFormationStats { get; set; } = new();
    }
}
