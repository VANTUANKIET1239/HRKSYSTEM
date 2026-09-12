using System;

namespace GAME.Domain.Entities
{
    public class HrkEnhancementLevelConfig
    {
        public int Id { get; set; }
        public int CurrentLevel { get; set; }
        public int NextLevel { get; set; }
        public decimal BaseSuccessRate { get; set; }
        public int GoldCost { get; set; }
        public int FailureDropLevels { get; set; } = 0;
        public int MaxStoneSlots { get; set; } = 3;
        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedOn { get; set; } = DateTime.UtcNow;
    }
}
