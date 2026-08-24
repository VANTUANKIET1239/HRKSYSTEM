using System;

namespace GAME.Domain.Entities
{
    public class HrkPlayerWallet
    {
        public long PlayerId { get; set; }
        public long Gold { get; set; } = 0;
        public int Diamonds { get; set; } = 0;
        public int UpgradeMaterials { get; set; } = 0;
        public int MaxCapacity { get; set; } = 200;
        public DateTime UpdatedOn { get; set; } = DateTime.UtcNow;

        public virtual HrkPlayer Player { get; set; } = null!;
    }
}
