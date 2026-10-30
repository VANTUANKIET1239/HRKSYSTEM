using System.Collections.Generic;

namespace GAME.Application.DTOs
{

    public class HeroUpgradePreviewDto
    {
        public long HeroId { get; set; }
        public int CurrentLevel { get; set; }
        public int NextLevel { get; set; }
        public int MaxLevel { get; set; }
        public long GoldCost { get; set; }
        public int MaterialCost { get; set; }
        public CalculatedStatsDto CurrentStats { get; set; } = new();
        public CalculatedStatsDto NextStats { get; set; } = new();
        public CalculatedStatsDto StatIncrease { get; set; } = new();
    }
}
