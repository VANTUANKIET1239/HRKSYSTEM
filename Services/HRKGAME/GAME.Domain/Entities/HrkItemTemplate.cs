using System;
using System.Collections.Generic;

namespace GAME.Domain.Entities
{
    public class HrkItemTemplate
    {
        public int Id { get; set; }
        public int CategoryId { get; set; }
        public int RarityId { get; set; }
        public string Name { get; set; } = null!;
        public string Icon { get; set; } = null!;
        public int LevelReq { get; set; } = 1;
        public string? Description { get; set; }
        public string? BaseStats { get; set; }
        public bool IsStackable { get; set; }
        public int MaxStackSize { get; set; } = 1;
        public int SellPrice { get; set; } = 100;
        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;

        public virtual HrkItemCategory Category { get; set; } = null!;
        public virtual HrkRarity Rarity { get; set; } = null!;
        public virtual ICollection<HrkPlayerInventory> Inventories { get; set; } = new List<HrkPlayerInventory>();
    }
}
