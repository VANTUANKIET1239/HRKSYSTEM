using System;
using System.Collections.Generic;

namespace GAME.Application.DTOs
{

    public class StartBattleResultDto
    {
        public string BattleId { get; set; } = null!;
        public int RandomSeed { get; set; }
        public string Status { get; set; } = "COMPLETED";
        public string Winner { get; set; } = null!;
        public BattleInitialStateDto InitialState { get; set; } = new();
        public List<BattleEventDto> Events { get; set; } = new();
        public List<BattleHeroStatisticsDto> HeroStatistics { get; set; } = new();
    }
}
