using GAME.Domain.Entities;
using GAME.Domain.Interfaces;
using GAME.Domain.ValueObjects;
using System;

namespace GAME.Domain.Services
{
    /// <summary>
    /// Triển khai Pure Domain Service cho logic Cường hóa trang bị.
    /// </summary>
    public class EquipmentEnhancementDomainService : IEquipmentEnhancementDomainService
    {
        private readonly IEnhancementRoller _roller;

        public EquipmentEnhancementDomainService(IEnhancementRoller roller)
        {
            _roller = roller ?? throw new ArgumentNullException(nameof(roller));
        }

        public EnhancementExecutionResult ExecuteAttempt(
            HrkPlayerInventory equipment,
            HrkEnhancementLevelConfig config,
            EnhancementMaterials materials)
        {
            if (equipment == null) throw new ArgumentNullException(nameof(equipment));
            if (config == null) throw new ArgumentNullException(nameof(config));
            materials ??= EnhancementMaterials.Empty;

            // 1. Kiểm tra bất biến tiên quyết của Aggregate
            equipment.EnsureCanBeEnhanced();

            if (config.CurrentLevel != equipment.Enhancement)
            {
                throw new InvalidOperationException($"Cấu hình cấp độ (+{config.CurrentLevel}) không khớp với cấp độ hiện tại của trang bị (+{equipment.Enhancement}).");
            }

            // 2. Tính toán tổng tỷ lệ thành công qua Value Object
            var finalRate = SuccessRate.From(config.BaseSuccessRate)
                .AddBonus(materials.TotalStoneSuccessBonus)
                .AddBonus(materials.CharmSuccessBonus)
                .CapAt100Percent();

            // 3. Thực hiện quay xúc xắc ngẫu nhiên (RNG)
            bool isSuccess = _roller.Roll(finalRate.Value);

            int oldLevel = equipment.Enhancement;
            int targetLevel = oldLevel + 1;
            int newLevel;
            int actualDropLevels;
            bool wasProtected;

            // 4. Áp dụng chuyển đổi trạng thái trên Aggregate Root
            if (isSuccess)
            {
                equipment.ApplyEnhancementSuccess();
                newLevel = equipment.Enhancement;
                actualDropLevels = 0;
                wasProtected = false;
            }
            else
            {
                if (materials.HasProtectionCharm)
                {
                    actualDropLevels = 0;
                    wasProtected = true;
                    newLevel = oldLevel;
                    // Bùa hộ mệnh kích hoạt, không giảm cấp độ
                }
                else
                {
                    actualDropLevels = config.FailureDropLevels;
                    wasProtected = false;
                    equipment.ApplyEnhancementFailure(actualDropLevels);
                    newLevel = equipment.Enhancement;
                }
            }

            return new EnhancementExecutionResult(
                IsSuccess: isSuccess,
                OldEnhancement: oldLevel,
                TargetEnhancement: targetLevel,
                NewEnhancement: newLevel,
                BaseSuccessRate: config.BaseSuccessRate,
                StoneBonusRate: materials.TotalStoneSuccessBonus,
                CharmBonusRate: materials.CharmSuccessBonus,
                FinalSuccessRate: finalRate.Value,
                FailureDropLevels: actualDropLevels,
                WasLevelProtected: wasProtected);
        }
    }
}
