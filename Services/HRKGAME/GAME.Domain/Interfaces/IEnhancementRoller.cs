namespace GAME.Domain.Interfaces
{
    /// <summary>
    /// Trừu tượng hóa việc gieo xúc xắc (RNG) trong cường hóa để đảm bảo bảo mật và khả năng Unit Test tất định.
    /// </summary>
    public interface IEnhancementRoller
    {
        /// <summary>
        /// Tung xúc xắc ngẫu nhiên và kiểm tra xem có rơi vào khoảng thành công [0, successRate) hay không.
        /// </summary>
        bool Roll(decimal successRate);

        /// <summary>
        /// Trả về một số thập phân ngẫu nhiên trong khoảng [0.0000, 1.0000).
        /// </summary>
        decimal NextRate();
    }
}
