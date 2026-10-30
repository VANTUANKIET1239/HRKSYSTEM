using System;
using System.Collections.Generic;

namespace GAME.Application.DTOs
{
    public class StartBattleRequestDto
    {
        public string BattleType { get; set; } = "PVE";
        public int? StageId { get; set; }
        public string? FormationCode { get; set; }
        public List<FormationPositionRequestDto>? Positions { get; set; }
        public int? RandomSeed { get; set; }
    }
}
