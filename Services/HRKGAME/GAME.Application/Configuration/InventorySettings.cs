namespace GAME.Application.Configuration
{
    public class InventorySettings
    {
        public const string SectionName = "InventorySettings";

        /// <summary>
        /// Sức chứa tối đa của túi hành trang người chơi.
        /// </summary>
        public int MaxBagCapacity { get; set; } = 500;

        /// <summary>
        /// Số Kim Cương tiêu hao cho mỗi 1 ô hành trang mở rộng.
        /// </summary>
        public int DiamondCostPerSlot { get; set; } = 20;

        /// <summary>
        /// Bước nhảy số ô mỗi lần mở rộng (ví dụ: bội số của 5).
        /// </summary>
        public int SlotStepIncrement { get; set; } = 5;
    }
}
