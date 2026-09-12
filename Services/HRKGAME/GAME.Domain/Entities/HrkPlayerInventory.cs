using System;

namespace GAME.Domain.Entities
{
    public class HrkPlayerInventory
    {
        public long Id { get; set; }
        public long PlayerId { get; set; }
        public int ItemTemplateId { get; set; }
        /// <summary>
        /// Số lượng vật phẩm. Đối với Trang bị (Equipment), Count luôn = 1 (mỗi trang bị là 1 dòng độc lập).
        /// Đối với vật phẩm xếp chồng (Hero Shard, Consumable, Material), Count >= 1 trong 1 dòng duy nhất.
        /// </summary>
        public int Count { get; set; } = 1;
        public int Enhancement { get; set; } = 0;
        public int Stars { get; set; } = 0;
        public bool IsEquipped { get; set; } = false;
        public long? EquippedHeroId { get; set; }
        public bool IsLocked { get; set; } = false;
        public bool IsActive { get; set; } = true;
        public int? SlotIndex { get; set; }

        /// <summary>
        /// JSON snapshot/cache chỉ số hiện tại đã tính toán (derived cache).
        /// KHÔNG phải là nguồn sự thật (Source of Truth).
        /// Nguồn sự thật là HRK_ItemTemplateAttributes + Enhancement + Stars (+ modifiers tương lai).
        /// </summary>
        public string? CurrentStats { get; set; }
        public DateTime AcquiredOn { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedOn { get; set; } = DateTime.UtcNow;

        public virtual HrkPlayer Player { get; set; } = null!;
        public virtual HrkItemTemplate ItemTemplate { get; set; } = null!;
        public virtual HrkPlayerHero? EquippedHero { get; set; }
    }
}
