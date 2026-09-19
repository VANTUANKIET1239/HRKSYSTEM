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

        #region Domain Behaviors

        public bool HasEnoughGold(long amount) => Gold >= amount;

        public void DeductGold(long amount)
        {
            if (amount < 0)
                throw new ArgumentOutOfRangeException(nameof(amount), "Số vàng cần trừ không được là số âm.");

            if (Gold < amount)
                throw new InvalidOperationException($"Số dư Vàng không đủ. Cần: {amount:N0} Vàng (Hiện có: {Gold:N0} Vàng).");

            Gold -= amount;
            UpdatedOn = DateTime.UtcNow;
        }

        public void AddGold(long amount)
        {
            if (amount < 0)
                throw new ArgumentOutOfRangeException(nameof(amount), "Số vàng cộng thêm không được là số âm.");

            Gold += amount;
            UpdatedOn = DateTime.UtcNow;
        }

        #endregion
    }
}
