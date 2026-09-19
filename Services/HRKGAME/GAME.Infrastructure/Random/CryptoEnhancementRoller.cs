using GAME.Domain.Interfaces;
using System.Security.Cryptography;

namespace GAME.Infrastructure.Random
{
    /// <summary>
    /// Triển khai bộ gieo xúc xắc sử dụng Cryptographic RNG của .NET để chống gian lận.
    /// </summary>
    public class CryptoEnhancementRoller : IEnhancementRoller
    {
        public bool Roll(decimal successRate)
        {
            return NextRate() < successRate;
        }

        public decimal NextRate()
        {
            // Sinh số nguyên ngẫu nhiên từ 0 đến 9999 (độ chính xác 4 chữ số thập phân 0.0001)
            int rollInt = RandomNumberGenerator.GetInt32(0, 10000);
            return rollInt / 10000.0m;
        }
    }
}
