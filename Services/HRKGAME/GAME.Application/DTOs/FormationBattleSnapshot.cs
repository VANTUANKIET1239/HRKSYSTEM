using System.Collections.Generic;

namespace GAME.Application.DTOs
{

    public class FormationBattleSnapshot
    {
        public long FormationId { get; set; }
        public string FormationCode { get; set; } = null!;
        public string FormationName { get; set; } = null!;
        public int FormationLevel { get; set; } = 1;
        public FormationStatBonusDto FormationBonus { get; set; } = new();
        public List<FormationSlotDto> Slots { get; set; } = new();
        public List<PlayerHeroDto> Heroes { get; set; } = new();
        public List<long> ParticipantHeroIds { get; set; } = new();
        public Dictionary<long, CalculatedStatsDto> FinalStatsByHero { get; set; } = new();
        public Dictionary<long, int> HeroPowers { get; set; } = new();
        public int BaseHeroPower { get; set; }
        public int FormationBonusPower { get; set; }
        public int TotalPower { get; set; }
    }
}
