using GAME.Application.DTOs;
using GAME.Domain.Entities;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GAME.Application.Interfaces
{
    public interface IInventoryService
    {
        Task<List<InventoryItemDto>> GetPlayerInventoryAsync(string userId, string? categoryCode, CancellationToken cancellationToken = default);
        Task<HeroEquipmentDto> GetHeroEquipmentAsync(string userId, long heroId, CancellationToken cancellationToken = default);
        Task<SellItemsResponseDto> SellItemsAsync(string userId, List<SellItemRequestItem> items, CancellationToken cancellationToken = default);
        Task<bool> ToggleItemLockAsync(string userId, long inventoryItemId, bool isLocked, CancellationToken cancellationToken = default);
        Task<PlayerWalletDto> ExpandCapacityAsync(string userId, int slotsToAdd, CancellationToken cancellationToken = default);
        Task<HrkPlayerInventory> AddItemToInventoryAsync(string userId, int itemTemplateId, int count = 1, CancellationToken cancellationToken = default);
    }
}
