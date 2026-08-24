using System;
using System.Collections.Generic;

namespace GAME.Domain.Entities
{
    public class HrkPlayerHero
    {
        public long Id { get; set; }
        public long PlayerId { get; set; }
        public int HeroTemplateId { get; set; }
        public int Level { get; set; } = 1;
        public int Exp { get; set; } = 0;
        public int MaxExp { get; set; } = 500;
        public int Stars { get; set; } = 1;
        public int Power { get; set; } = 1000;
        public byte AuraTier { get; set; } = 1;
        public bool IsLocked { get; set; } = false;
        public bool IsFavorite { get; set; } = false;
        public string? CurrentStats { get; set; }
        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedOn { get; set; } = DateTime.UtcNow;

        public virtual HrkPlayer Player { get; set; } = null!;
        public virtual HrkHeroTemplate HeroTemplate { get; set; } = null!;

        public virtual ICollection<HrkPlayerEquipment> EquippedOnHeroes { get; set; } = new List<HrkPlayerEquipment>();
        public virtual ICollection<HrkPlayerInventory> EquippedItems { get; set; } = new List<HrkPlayerInventory>();
    }
}
