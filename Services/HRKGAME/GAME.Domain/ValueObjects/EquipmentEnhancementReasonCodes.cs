namespace GAME.Domain.ValueObjects
{
    /// <summary>
    /// Các mã lý do chuẩn hóa cho kết quả xác thực điều kiện cường hóa trang bị.
    /// </summary>
    public static class EquipmentEnhancementReasonCodes
    {
        public const string Ok = "OK";
        public const string Inactive = "INACTIVE";
        public const string InvalidCategory = "INVALID_CATEGORY";
        public const string NotEquipment = "NOT_EQUIPMENT";
        public const string Locked = "LOCKED";
        public const string MaxEnhancement = "MAX_ENHANCEMENT";
    }
}
