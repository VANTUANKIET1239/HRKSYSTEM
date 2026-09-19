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

    /// <summary>
    /// Value Object biểu thị kết quả kiểm tra tính hợp lệ của trang bị cho tính năng cường hóa.
    /// Cho phép truy vấn tính hợp lệ mà không cần ném ngoại lệ (dành cho UI/Query Model).
    /// </summary>
    public sealed class EquipmentEnhancementEligibility
    {
        public bool CanEnhance { get; }
        public string ReasonCode { get; }
        public string? Message { get; }

        private EquipmentEnhancementEligibility(bool canEnhance, string reasonCode, string? message)
        {
            CanEnhance = canEnhance;
            ReasonCode = reasonCode;
            Message = message;
        }

        public static EquipmentEnhancementEligibility Success()
            => new(true, EquipmentEnhancementReasonCodes.Ok, null);

        public static EquipmentEnhancementEligibility Fail(string reasonCode, string message)
            => new(false, reasonCode, message);
    }
}
