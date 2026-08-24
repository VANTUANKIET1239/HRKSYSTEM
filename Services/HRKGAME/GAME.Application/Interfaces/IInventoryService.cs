using GAME.Application.DTOs;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GAME.Application.Interfaces
{
    public interface IInventoryService
    {
        Task<List<InventoryItemDto>> GetPlayerInventoryAsync(string userId, string? categoryCode, CancellationToken cancellationToken = default);
        Task<HeroEquipmentDto> GetHeroEquipmentAsync(string userId, long heroId, CancellationToken cancellationToken = default);
    }
}
