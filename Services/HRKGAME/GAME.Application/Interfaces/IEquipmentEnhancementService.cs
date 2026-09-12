using GAME.Application.DTOs;
using System.Threading;
using System.Threading.Tasks;

namespace GAME.Application.Interfaces
{
    public interface IEquipmentEnhancementService
    {
        Task<EnhanceEquipmentResultDto> EnhanceEquipmentAsync(string userId, EnhanceEquipmentRequestDto request, CancellationToken cancellationToken = default);
        Task<EnhancementConfigResponseDto> GetEnhancementConfigsAsync(CancellationToken cancellationToken = default);
    }
}
