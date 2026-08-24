using Core.Common.Repositories;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;
using GAME.Domain.Entities;
using GAME.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace GAME.Infrastructure.Services
{
    public class InventoryService : IInventoryService
    {
        private readonly IUnitOfWork<GameDbContext> _unitOfWork;

        public InventoryService(IUnitOfWork<GameDbContext> unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<List<InventoryItemDto>> GetPlayerInventoryAsync(string userId, string? categoryCode, CancellationToken cancellationToken = default)
        {
            var player = await _unitOfWork.Repository<HrkPlayer>().Query()
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);

            if (player == null) return new List<InventoryItemDto>();

            var query = _unitOfWork.Repository<HrkPlayerInventory>().Query()
                .AsNoTracking()
                .Where(inv => inv.PlayerId == player.Id && !inv.IsEquipped)
                .Include(inv => inv.ItemTemplate).ThenInclude(it => it.Category)
                .Include(inv => inv.ItemTemplate).ThenInclude(it => it.Rarity)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(categoryCode))
            {
                query = query.Where(inv => inv.ItemTemplate.Category.Code.ToLower() == categoryCode.ToLower());
            }

            var items = await query.ToListAsync(cancellationToken);

            return items.Select(inv => MapItem(inv)!).ToList();
        }

        public async Task<HeroEquipmentDto> GetHeroEquipmentAsync(string userId, long heroId, CancellationToken cancellationToken = default)
        {
            var player = await _unitOfWork.Repository<HrkPlayer>().Query()
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);

            if (player == null)
            {
                return new HeroEquipmentDto { HeroId = heroId };
            }

            var eq = await _unitOfWork.Repository<HrkPlayerEquipment>().Query()
                .AsNoTracking()
                .Include(e => e.Weapon).ThenInclude(w => w!.ItemTemplate).ThenInclude(it => it.Rarity)
                .Include(e => e.Weapon).ThenInclude(w => w!.ItemTemplate).ThenInclude(it => it.Category)
                .Include(e => e.Armor).ThenInclude(a => a!.ItemTemplate).ThenInclude(it => it.Rarity)
                .Include(e => e.Armor).ThenInclude(a => a!.ItemTemplate).ThenInclude(it => it.Category)
                .Include(e => e.Helmet).ThenInclude(h => h!.ItemTemplate).ThenInclude(it => it.Rarity)
                .Include(e => e.Helmet).ThenInclude(h => h!.ItemTemplate).ThenInclude(it => it.Category)
                .Include(e => e.Boots).ThenInclude(b => b!.ItemTemplate).ThenInclude(it => it.Rarity)
                .Include(e => e.Boots).ThenInclude(b => b!.ItemTemplate).ThenInclude(it => it.Category)
                .Include(e => e.Ring).ThenInclude(r => r!.ItemTemplate).ThenInclude(it => it.Rarity)
                .Include(e => e.Ring).ThenInclude(r => r!.ItemTemplate).ThenInclude(it => it.Category)
                .Include(e => e.Artifact).ThenInclude(ar => ar!.ItemTemplate).ThenInclude(it => it.Rarity)
                .Include(e => e.Artifact).ThenInclude(ar => ar!.ItemTemplate).ThenInclude(it => it.Category)
                .FirstOrDefaultAsync(e => e.PlayerId == player.Id && e.HeroId == heroId, cancellationToken);

            return new HeroEquipmentDto
            {
                HeroId = heroId,
                Weapon = MapItem(eq?.Weapon),
                Armor = MapItem(eq?.Armor),
                Helmet = MapItem(eq?.Helmet),
                Boots = MapItem(eq?.Boots),
                Ring = MapItem(eq?.Ring),
                Artifact = MapItem(eq?.Artifact)
            };
        }

        private static InventoryItemDto? MapItem(HrkPlayerInventory? inv)
        {
            if (inv == null || inv.ItemTemplate == null) return null;
            var it = inv.ItemTemplate;
            return new InventoryItemDto
            {
                Id = inv.Id,
                ItemTemplateId = inv.ItemTemplateId,
                Name = it.Name,
                Icon = it.Icon,
                RarityId = it.RarityId,
                RarityCode = it.Rarity?.Code ?? "",
                RarityName = it.Rarity?.Name ?? "",
                RarityColorHex = it.Rarity?.ColorHex,
                CategoryId = it.CategoryId,
                CategoryCode = it.Category?.Code ?? "",
                CategoryName = it.Category?.Name ?? "",
                IsEquipment = it.Category?.IsEquipment ?? false,
                Count = inv.Count,
                LevelReq = it.LevelReq,
                Description = it.Description,
                Stats = ParseJson(inv.CurrentStats ?? it.BaseStats),
                IsLocked = inv.IsLocked,
                IsEquipped = inv.IsEquipped,
                EquippedHeroId = inv.EquippedHeroId,
                Enhancement = inv.Enhancement,
                Stars = inv.Stars,
                SlotIndex = inv.SlotIndex
            };
        }

        private static object? ParseJson(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            try { return JsonSerializer.Deserialize<object>(json); }
            catch { return json; }
        }
    }
}
