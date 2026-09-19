using System;

namespace GAME.Domain.Entities
{
    public class HrkCombatPowerConfig
    {
        public int Id { get; set; }
        public string StatCode { get; set; } = null!;
        public decimal PowerPerUnit { get; set; }
        public bool IsEnabled { get; set; } = true;
        public int DisplayOrder { get; set; }
        public DateTime UpdatedOn { get; set; } = DateTime.UtcNow;
    }
}
