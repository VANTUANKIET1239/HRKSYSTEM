using System;

namespace GAME.Application.DTOs
{

    public class PlayerWalletDto
    {
        public long PlayerId { get; set; }
        public long Gold { get; set; }
        public int Diamonds { get; set; }
        public int UpgradeMaterials { get; set; }
        public int MaxCapacity { get; set; }
        public DateTime UpdatedOn { get; set; }
    }
}
