using GAME.Application.DTOs;
using System.Threading;
using System.Threading.Tasks;

namespace GAME.Application.Interfaces
{
    public interface IHeroEquipmentService
    {
        Task<PlayerHeroDetailDto> EquipHeroItemAsync(string userId, long heroId, long inventoryItemId, CancellationToken cancellationToken = default);
        Task<PlayerHeroDetailDto> UnequipHeroItemAsync(string userId, long heroId, string slotCode, CancellationToken cancellationToken = default);
    }
}
