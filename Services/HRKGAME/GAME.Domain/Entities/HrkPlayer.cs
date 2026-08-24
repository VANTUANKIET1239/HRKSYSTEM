using System;
using System.Collections.Generic;

namespace GAME.Domain.Entities
{
    public class HrkPlayer
    {
        public long Id { get; set; }
        public string UserId { get; set; } = null!;
        public string PlayerName { get; set; } = null!;
        public int Level { get; set; } = 1;
        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedOn { get; set; } = DateTime.UtcNow;

        public virtual HrkUser? User { get; set; }
        public virtual HrkPlayerWallet? Wallet { get; set; }
        public virtual ICollection<HrkPlayerHero> Heroes { get; set; } = new List<HrkPlayerHero>();
        public virtual ICollection<HrkPlayerInventory> Inventories { get; set; } = new List<HrkPlayerInventory>();
        public virtual ICollection<HrkPlayerFormation> Formations { get; set; } = new List<HrkPlayerFormation>();
        public virtual ICollection<HrkPlayerEquipment> Equipments { get; set; } = new List<HrkPlayerEquipment>();
    }
}
