using GAME.Domain.Entities;
using GAME.Domain.ValueObjects;

namespace GAME.Domain.Services
{
    /// <summary>
    /// Domain Service chịu trách nhiệm xử lý các quy tắc nghiệp vụ cốt lõi của quá trình cường hóa trang bị
    /// (tính toán tỷ lệ thành công, gieo xúc xắc RNG, kích hoạt hiệu ứng bảo vệ bùa và thay đổi trạng thái thực thể).
    /// </summary>
    public interface IEquipmentEnhancementDomainService
    {
        EnhancementExecutionResult ExecuteAttempt(
            HrkPlayerInventory equipment,
            HrkEnhancementLevelConfig config,
            EnhancementMaterials materials);
    }
}
