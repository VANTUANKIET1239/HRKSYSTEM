using System;

namespace GAME.Application.DTOs
{
    public class PlayerProfileDto
    {
        public long Id { get; set; }
        public string UserId { get; set; } = null!;
        public string PlayerName { get; set; } = null!;
        public int Level { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime UpdatedOn { get; set; }
    }

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
