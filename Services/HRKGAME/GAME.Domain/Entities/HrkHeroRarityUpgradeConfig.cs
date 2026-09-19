using System;

namespace GAME.Domain.Entities
{
    /// <summary>Balance configuration for hero level-up, one row per hero rarity.</summary>
    public class HrkHeroRarityUpgradeConfig
    {
        public int Id { get; set; }
        public int RarityId { get; set; }
        public decimal StatGrowthRate { get; set; }
        public long BaseGoldCost { get; set; }
        public long GoldCostPerLevel { get; set; }
        public int BaseMaterialCost { get; set; }
        public int MaterialCostPerLevel { get; set; }
        public int MaxLevel { get; set; } = 100;
        public DateTime UpdatedOn { get; set; } = DateTime.UtcNow;

        public virtual HrkRarity Rarity { get; set; } = null!;
    }
}
