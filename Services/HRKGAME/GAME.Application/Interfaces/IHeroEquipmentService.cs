using GAME.Application.DTOs;
using System.Threading;
using System.Threading.Tasks;

namespace GAME.Application.Interfaces
{
    public interface IHeroEquipmentService
    {
        Task<PlayerHeroDetailDto> EquipHeroItemAsync(string userId, long heroId, long inventoryItemId, CancellationToken cancellationToken = default);
        Task<PlayerHeroDetailDto> UnequipHeroItemAsync(string userId, long heroId, string slotCode, CancellationToken cancellationToken = default);
        Task<PlayerHeroDetailDto> UnequipAllHeroItemsAsync(string userId, long heroId, CancellationToken cancellationToken = default);
        Task<SwapHeroEquipmentResultDto> SwapHeroEquipmentAsync(string userId, long sourceHeroId, long targetHeroId, CancellationToken cancellationToken = default);
    }
}
