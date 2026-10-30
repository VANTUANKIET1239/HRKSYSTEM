using System;
using System.Collections.Generic;

namespace GAME.Application.DTOs
{

    public class EnhancementLevelConfigDto
    {
        public int CurrentLevel { get; set; }
        public int NextLevel { get; set; }
        public decimal BaseSuccessRate { get; set; }
        public int GoldCost { get; set; }
        public int FailureDropLevels { get; set; }
        public int MaxStoneSlots { get; set; }
    }
}
