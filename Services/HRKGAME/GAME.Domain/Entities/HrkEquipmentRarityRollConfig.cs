using System;

namespace GAME.Domain.Entities
{
    public class HrkEquipmentRarityRollConfig
    {
        public int Id { get; set; }
        public int RarityId { get; set; }
        public decimal EnhancementGrowthMinPercent { get; set; }
        public decimal EnhancementGrowthMaxPercent { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedOn { get; set; } = DateTime.UtcNow;

        public virtual HrkRarity Rarity { get; set; } = null!;
    }
}
