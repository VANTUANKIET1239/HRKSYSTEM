using System;

namespace GAME.Domain.Entities
{
    public class HrkPlayerInventory
    {
        public long Id { get; set; }
        public long PlayerId { get; set; }
        public int ItemTemplateId { get; set; }
        public int Count { get; set; } = 1;
        public int Enhancement { get; set; } = 0;
        public int Stars { get; set; } = 0;
        public bool IsEquipped { get; set; } = false;
        public long? EquippedHeroId { get; set; }
        public bool IsLocked { get; set; } = false;
        public int? SlotIndex { get; set; }
        public string? CurrentStats { get; set; }
        public DateTime AcquiredOn { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedOn { get; set; } = DateTime.UtcNow;

        public virtual HrkPlayer Player { get; set; } = null!;
        public virtual HrkItemTemplate ItemTemplate { get; set; } = null!;
        public virtual HrkPlayerHero? EquippedHero { get; set; }
    }
}
