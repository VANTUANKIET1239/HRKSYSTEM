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
        public int Exp { get; set; }
        public int MaxExp { get; set; } = 1000;
        public int Stamina { get; set; } = 200;
        public int MaxStamina { get; set; } = 200;
        public DateTime LastStaminaRegeneratedOn { get; set; } = DateTime.UtcNow;
        public int DailyStaminaPurchaseCount { get; set; }
        public DateTime? StaminaPurchaseDate { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedOn { get; set; } = DateTime.UtcNow;
        public string AvatarType { get; set; } = "TEMPLATE";
        public int? AvatarTemplateId { get; set; }

        public virtual HrkUser? User { get; set; }
        public virtual HrkPlayerWallet? Wallet { get; set; }
        public virtual HrkAvatarTemplate? AvatarTemplate { get; set; }
        public virtual HrkPlayerCustomAvatar? CustomAvatar { get; set; }
        public virtual ICollection<HrkPlayerHero> Heroes { get; set; } = new List<HrkPlayerHero>();
        public virtual ICollection<HrkPlayerInventory> Inventories { get; set; } = new List<HrkPlayerInventory>();
        public virtual ICollection<HrkPlayerFormation> Formations { get; set; } = new List<HrkPlayerFormation>();
        public virtual ICollection<HrkPlayerEquipment> Equipments { get; set; } = new List<HrkPlayerEquipment>();
        public virtual ICollection<HrkPlayerDungeonStageProgress> DungeonProgress { get; set; } = new List<HrkPlayerDungeonStageProgress>();
        public virtual ICollection<HrkDungeonRun> DungeonRuns { get; set; } = new List<HrkDungeonRun>();
        public virtual ICollection<HrkPlayerDungeonStarChestClaim> StarChestClaims { get; set; } = new List<HrkPlayerDungeonStarChestClaim>();
    }
}
