using System.Collections.Generic;

namespace GAME.Application.DTOs
{

    public class HeroStarUpgradePreviewDto
    {
        public long HeroId { get; set; }
        public int CurrentStar { get; set; }
        public int NextStar { get; set; }
        public int CurrentLevel { get; set; }
        public int MaxStar { get; set; } = 5;
        public long GoldRequired { get; set; }
        public long GoldOwned { get; set; }
        public StarMaterialRequirementDto UniversalStone { get; set; } = new();
        public StarMaterialRequirementDto HeroStone { get; set; } = new();
        public CalculatedStatsDto CurrentStats { get; set; } = new();
        public CalculatedStatsDto NextStats { get; set; } = new();
        public CalculatedStatsDto StatIncrease { get; set; } = new();
        public decimal CurrentGrowthRate { get; set; }
        public decimal NextGrowthRate { get; set; }
        public List<HeroBonusAttributeDto> CurrentBonusAttributes { get; set; } = new();
        public bool WillUnlockBonusAttribute { get; set; }
        public int CurrentCombatPower { get; set; }
        public int NextCombatPower { get; set; }
        public bool CanUpgrade { get; set; }
        public string? ReasonCode { get; set; }
        public string? Message { get; set; }
    }
}
