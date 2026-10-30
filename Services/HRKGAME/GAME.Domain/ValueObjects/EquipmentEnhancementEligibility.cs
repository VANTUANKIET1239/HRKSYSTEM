namespace GAME.Domain.ValueObjects
{

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
