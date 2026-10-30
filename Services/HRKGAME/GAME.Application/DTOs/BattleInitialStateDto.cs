using System;
using System.Collections.Generic;

namespace GAME.Application.DTOs
{

    public class BattleInitialStateDto
    {
        public string BattleId { get; set; } = null!;
        public List<PlayerHeroDto> LeftTeam { get; set; } = new();
        public List<PlayerHeroDto> RightTeam { get; set; } = new();
    }
}
