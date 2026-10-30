using System.Collections.Generic;

namespace GAME.Application.DTOs
{

    public class FormationPowerResultDto
    {
        public long FormationId { get; set; }
        public string FormationCode { get; set; } = "";
        public string FormationName { get; set; } = "";
        public int FormationLevel { get; set; }
        public int HeroCount { get; set; }
        public int BaseHeroPower { get; set; }
        public int FormationBonusPower { get; set; }
        public int TotalPower { get; set; }
        public List<long> PlacedHeroIds { get; set; } = new();
        public Dictionary<long, int> HeroTotalPowers { get; set; } = new();
    }
}
