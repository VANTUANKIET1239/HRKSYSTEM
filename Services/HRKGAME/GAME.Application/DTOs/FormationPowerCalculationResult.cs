using System.Collections.Generic;

namespace GAME.Application.DTOs
{

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
