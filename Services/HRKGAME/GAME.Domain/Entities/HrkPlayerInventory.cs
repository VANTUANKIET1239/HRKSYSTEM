using GAME.Domain.Exceptions;
using GAME.Domain.ValueObjects;
using System;

namespace GAME.Domain.Entities
{
    public class HrkPlayerInventory
    {
        public long Id { get; set; }
        public long PlayerId { get; set; }
        public int ItemTemplateId { get; set; }
        /// <summary>
        /// Số lượng vật phẩm. Đối với Trang bị (Equipment), Count luôn = 1 (mỗi trang bị là 1 dòng độc lập).
        /// Đối với vật phẩm xếp chồng (Hero Shard, Consumable, Material), Count >= 1 trong 1 dòng duy nhất.
        /// </summary>
        public int Count { get; set; } = 1;
        public int Enhancement { get; set; } = 0;
        public int Stars { get; set; } = 0;
        public bool IsEquipped { get; set; } = false;
        public long? EquippedHeroId { get; set; }
        public bool IsLocked { get; set; } = false;
        public bool IsActive { get; set; } = true;
        public int? SlotIndex { get; set; }

        /// <summary>
        /// JSON snapshot/cache chỉ số hiện tại đã tính toán (derived cache).
        /// KHÔNG phải là nguồn sự thật (Source of Truth).
        /// Nguồn sự thật là HRK_ItemTemplateAttributes + Enhancement + Stars (+ modifiers tương lai).
        /// </summary>
        public string? CurrentStats { get; set; }
        public DateTime AcquiredOn { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedOn { get; set; } = DateTime.UtcNow;

        public virtual HrkPlayer Player { get; set; } = null!;
        public virtual HrkItemTemplate ItemTemplate { get; set; } = null!;
        public virtual HrkPlayerHero? EquippedHero { get; set; }

        #region Domain Behaviors & Invariant Protection

        /// <summary>
        /// Kiểm tra tính hợp lệ của trang bị cho tính năng cường hóa mà không ném ngoại lệ.
        /// Sử dụng cho truy vấn đọc (Query/UI) và làm nền tảng cho EnsureCanBeEnhanced().
        /// </summary>
        public EquipmentEnhancementEligibility CheckEnhancementEligibility()
        {
            if (!IsActive)
            {
                return EquipmentEnhancementEligibility.Fail(
                    EquipmentEnhancementReasonCodes.Inactive,
                    "Trang bị không còn khả dụng trong hành trang.");
            }

            if (ItemTemplate?.Category == null)
            {
                return EquipmentEnhancementEligibility.Fail(
                    EquipmentEnhancementReasonCodes.InvalidCategory,
                    "Trang bị không có danh mục hợp lệ.");
            }

            if (!ItemTemplate.Category.IsEquipment)
            {
                return EquipmentEnhancementEligibility.Fail(
                    EquipmentEnhancementReasonCodes.NotEquipment,
                    "Vật phẩm này không phải là trang bị có thể cường hóa.");
            }

            if (IsLocked)
            {
                return EquipmentEnhancementEligibility.Fail(
                    EquipmentEnhancementReasonCodes.Locked,
                    "Trang bị đang bị khóa, vui lòng mở khóa trước khi cường hóa.");
            }

            if (Enhancement >= 15)
            {
                return EquipmentEnhancementEligibility.Fail(
                    EquipmentEnhancementReasonCodes.MaxEnhancement,
                    "Trang bị đã đạt cấp cường hóa tối đa (+15). Không thể cường hóa thêm.");
            }

            return EquipmentEnhancementEligibility.Success();
        }

        /// <summary>
        /// Xác thực điều kiện tiên quyết và bảo vệ bất biến của Aggregate trước khi thực thi lệnh cường hóa.
        /// Tái sử dụng cùng quy tắc từ CheckEnhancementEligibility() và ném DomainException nếu không hợp lệ.
        /// </summary>
        public void EnsureCanBeEnhanced()
        {
            var eligibility = CheckEnhancementEligibility();
            if (!eligibility.CanEnhance)
            {
                throw new DomainException(eligibility.ReasonCode, eligibility.Message ?? "Trang bị không thể cường hóa.");
            }
        }

        /// <summary>
        /// Áp dụng kết quả cường hóa thành công: tăng cấp cường hóa lên 1 (bảo vệ bất biến không vượt quá +15).
        /// </summary>
        public void ApplyEnhancementSuccess()
        {
            EnsureCanBeEnhanced();
            Enhancement = Math.Min(15, Enhancement + 1);
            UpdatedOn = DateTime.UtcNow;
        }

        /// <summary>
        /// Tăng cấp cường hóa lên 1 khi thành công (alias cho ApplyEnhancementSuccess).
        /// </summary>
        public void IncreaseEnhancement() => ApplyEnhancementSuccess();

        /// <summary>
        /// Áp dụng kết quả cường hóa thất bại: tụt cấp theo quy định (không bao giờ tụt dưới 0).
        /// </summary>
        public void ApplyEnhancementFailure(int dropLevels)
        {
            if (dropLevels < 0)
                throw new ArgumentOutOfRangeException(nameof(dropLevels), "Số cấp tụt không thể là số âm.");

            Enhancement = Math.Max(0, Enhancement - dropLevels);
            UpdatedOn = DateTime.UtcNow;
        }

        /// <summary>
        /// Cập nhật chuỗi JSON cache chỉ số dẫn xuất (snapshot cache).
        /// </summary>
        public void UpdateCurrentStatsCache(string? currentStatsJson)
        {
            CurrentStats = currentStatsJson;
            UpdatedOn = DateTime.UtcNow;
        }

        /// <summary>
        /// Tiêu hao số lượng vật phẩm (dành cho đá cường hóa, bùa hoặc nguyên liệu).
        /// Nếu số lượng về 0, chuyển trạng thái IsActive = false.
        /// </summary>
        public void ConsumeQuantity(int quantity)
        {
            if (quantity <= 0)
                throw new ArgumentOutOfRangeException(nameof(quantity), "Số lượng tiêu hao phải lớn hơn 0.");

            if (Count < quantity)
                throw new InvalidOperationException($"Số lượng vật phẩm '{ItemTemplate?.Name ?? "ID: " + Id}' trong túi không đủ ({Count}/{quantity}).");

            Count -= quantity;
            if (Count <= 0)
            {
                Count = 0;
                IsActive = false;
            }
            UpdatedOn = DateTime.UtcNow;
        }

        #endregion
    }
}
