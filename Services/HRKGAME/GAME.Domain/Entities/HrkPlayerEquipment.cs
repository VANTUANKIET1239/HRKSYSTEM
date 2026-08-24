namespace GAME.Domain.Entities
{
    public class HrkPlayerEquipment
    {
        public long PlayerId { get; set; }
        public long HeroId { get; set; }
        public long? WeaponId { get; set; }
        public long? ArmorId { get; set; }
        public long? HelmetId { get; set; }
        public long? BootsId { get; set; }
        public long? RingId { get; set; }
        public long? ArtifactId { get; set; }

        public virtual HrkPlayer Player { get; set; } = null!;
        public virtual HrkPlayerHero Hero { get; set; } = null!;
        public virtual HrkPlayerInventory? Weapon { get; set; }
        public virtual HrkPlayerInventory? Armor { get; set; }
        public virtual HrkPlayerInventory? Helmet { get; set; }
        public virtual HrkPlayerInventory? Boots { get; set; }
        public virtual HrkPlayerInventory? Ring { get; set; }
        public virtual HrkPlayerInventory? Artifact { get; set; }
    }
}
