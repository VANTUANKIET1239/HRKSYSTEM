using Core.Common.Repositories;
using GAME.Application.Common.Mappings;
using GAME.Application.Configuration;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;
using GAME.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GAME.Infrastructure.Services
{
    public class InventoryService : IInventoryService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IGamePlayerService _gamePlayerService;
        private readonly IItemStatCalculationService _statCalculationService;
        private readonly InventorySettings _settings;

        public InventoryService(
            IUnitOfWork unitOfWork,
            IGamePlayerService gamePlayerService,
            IItemStatCalculationService statCalculationService,
            IOptions<InventorySettings>? options = null)
        {
            _unitOfWork = unitOfWork;
            _gamePlayerService = gamePlayerService;
            _statCalculationService = statCalculationService;
            _settings = options?.Value ?? new InventorySettings();
        }

        public async Task<List<InventoryItemDto>> GetPlayerInventoryAsync(string userId, string? categoryCode, CancellationToken cancellationToken = default)
        {
            var player = await _gamePlayerService.GetPlayerByUserIdAsync(userId, cancellationToken);
            if (player == null) return new List<InventoryItemDto>();

            var query = _unitOfWork.ReadOnlyRepository<HrkPlayerInventory>().Query()
                .Where(inv => inv.PlayerId == player.Id && inv.IsActive)
                .Include(inv => inv.Attributes).ThenInclude(a => a.AttributeType)
                .Include(inv => inv.ItemTemplate).ThenInclude(it => it.Category)
                .Include(inv => inv.ItemTemplate).ThenInclude(it => it.Rarity)
                .Include(inv => inv.ItemTemplate).ThenInclude(it => it.Attributes).ThenInclude(a => a.AttributeType)
                .Include(inv => inv.EquippedHero).ThenInclude(hero => hero!.HeroTemplate)
                .OrderByDescending(x => x.ItemTemplate.RarityId)
                .AsSplitQuery()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(categoryCode) && !categoryCode.Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(inv => inv.ItemTemplate.Category.Code == categoryCode);
            }

            var items = await query.ToListAsync(cancellationToken);

            return items.Select(inv => GameDtoMapper.MapInventoryItem(inv)!).ToList();
        }

        public async Task<List<InventoryItemDto>> GetPlayerEquipmentAsync(string userId, string? categoryCode, bool includeEquipped = false, CancellationToken cancellationToken = default)
        {
            var player = await _gamePlayerService.GetPlayerByUserIdAsync(userId, cancellationToken);
            if (player == null) return new List<InventoryItemDto>();

            var query = _unitOfWork.ReadOnlyRepository<HrkPlayerInventory>().Query()
                .Where(inv => inv.PlayerId == player.Id && inv.IsActive && inv.ItemTemplate.Category.IsEquipment)
                .Include(inv => inv.Attributes).ThenInclude(a => a.AttributeType)
                .Include(inv => inv.ItemTemplate).ThenInclude(it => it.Category)
                .Include(inv => inv.ItemTemplate).ThenInclude(it => it.Rarity)
                .Include(inv => inv.ItemTemplate).ThenInclude(it => it.Attributes).ThenInclude(a => a.AttributeType)
                .Include(inv => inv.EquippedHero).ThenInclude(hero => hero!.HeroTemplate)
                .AsSplitQuery()
                .AsQueryable();

            if (!includeEquipped)
            {
                query = query.Where(inv => !inv.IsEquipped);
            }

            if (!string.IsNullOrWhiteSpace(categoryCode) && !categoryCode.Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                var upperCategoryCode = categoryCode.ToUpper();
                query = query.Where(inv => inv.ItemTemplate.Category.Code.ToUpper() == upperCategoryCode);
            }

            query = query
                .OrderByDescending(x => x.ItemTemplate.RarityId)
                .ThenByDescending(x => x.Enhancement)
                .ThenBy(x => x.ItemTemplate.Name);

            var items = await query.ToListAsync(cancellationToken);

            return items.Select(inv => GameDtoMapper.MapInventoryItem(inv)!).ToList();
        }

        public async Task<HeroEquipmentDto> GetHeroEquipmentAsync(string userId, long heroId, CancellationToken cancellationToken = default)
        {
            var player = await _gamePlayerService.GetPlayerByUserIdAsync(userId, cancellationToken);
            if (player == null)
            {
                return new HeroEquipmentDto { HeroId = heroId };
            }

            var equipments = await EquipmentBatchLoader.LoadForHeroesAsync(
                _unitOfWork, player.Id, new[] { heroId }, cancellationToken);
            equipments.TryGetValue(heroId, out var eq);

            return GameDtoMapper.MapHeroEquipment(eq, heroId);
        }

        public async Task<SellItemsResponseDto> SellItemsAsync(string userId, List<SellItemRequestItem> items, CancellationToken cancellationToken = default)
        {
            if (items == null || !items.Any())
            {
                throw new ArgumentException("Danh sách vật phẩm cần bán không được để trống.");
            }

            // Gộp các yêu cầu trùng lặp và loại bỏ các yêu cầu có số lượng <= 0
            var groupedItems = items
                .Where(i => i != null && i.Count > 0)
                .GroupBy(i => i.InventoryItemId)
                .Select(g => new SellItemRequestItem { InventoryItemId = g.Key, Count = g.Sum(x => x.Count) })
                .ToList();

            if (!groupedItems.Any())
            {
                throw new ArgumentException("Không có vật phẩm hợp lệ để bán.");
            }

            var player = await _gamePlayerService.GetPlayerByUserIdAsync(userId, cancellationToken);
            if (player == null)
            {
                throw new KeyNotFoundException("Không tìm thấy thông tin người chơi.");
            }

            var wallet = await _gamePlayerService.GetWalletByPlayerIdAsync(player.Id, cancellationToken);
            if (wallet == null)
            {
                throw new KeyNotFoundException("Không tìm thấy ví người chơi.");
            }

            var itemIds = groupedItems.Select(i => i.InventoryItemId).ToList();
            var inventories = await _unitOfWork.Repository<HrkPlayerInventory>().Query()
                .Include(i => i.ItemTemplate)
                .Where(i => i.PlayerId == player.Id && itemIds.Contains(i.Id) && i.IsActive)
                .ToListAsync(cancellationToken);

            long totalEarnedGold = 0;
            int totalSoldCount = 0;

            foreach (var req in groupedItems)
            {
                var inv = inventories.FirstOrDefault(i => i.Id == req.InventoryItemId);
                if (inv == null)
                {
                    throw new KeyNotFoundException($"Không tìm thấy vật phẩm với mã ID {req.InventoryItemId} trong hành trang.");
                }

                if (inv.IsLocked)
                {
                    throw new InvalidOperationException($"Vật phẩm '{inv.ItemTemplate.Name}' đang bị khóa, không thể bán.");
                }

                if (inv.IsEquipped)
                {
                    throw new InvalidOperationException($"Vật phẩm '{inv.ItemTemplate.Name}' đang được trang bị, không thể bán.");
                }

                if (req.Count > inv.Count)
                {
                    throw new InvalidOperationException($"Số lượng bán ({req.Count}) vượt quá số lượng hiện có ({inv.Count}) của '{inv.ItemTemplate.Name}'.");
                }

                if (inv.ItemTemplate.SellPrice < 0)
                {
                    throw new InvalidOperationException($"Vật phẩm '{inv.ItemTemplate.Name}' là vật phẩm không thể bán.");
                }

                // Sử dụng trực tiếp giá bán SellPrice được cấu hình trong bảng HRK_ItemTemplates
                long unitPrice = inv.ItemTemplate.SellPrice;

                totalEarnedGold += unitPrice * req.Count;
                totalSoldCount += req.Count;

                // Soft-delete: nếu bán hết thì chuyển IsActive = false, không xóa dòng trong DB
                if (inv.Count == req.Count)
                {
                    inv.Count = 0;
                    inv.IsActive = false;
                    inv.UpdatedOn = DateTime.UtcNow;
                    _unitOfWork.Repository<HrkPlayerInventory>().Update(inv);
                }
                else
                {
                    inv.Count -= req.Count;
                    inv.UpdatedOn = DateTime.UtcNow;
                    _unitOfWork.Repository<HrkPlayerInventory>().Update(inv);
                }
            }

            wallet.Gold += totalEarnedGold;
            wallet.UpdatedOn = DateTime.UtcNow;
            _unitOfWork.Repository<HrkPlayerWallet>().Update(wallet);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new SellItemsResponseDto
            {
                EarnedGold = totalEarnedGold,
                CurrentGold = wallet.Gold,
                SoldItemsCount = totalSoldCount
            };
        }

        public async Task<bool> ToggleItemLockAsync(string userId, long inventoryItemId, bool isLocked, CancellationToken cancellationToken = default)
        {
            var player = await _gamePlayerService.GetPlayerByUserIdAsync(userId, cancellationToken);
            if (player == null)
            {
                throw new KeyNotFoundException("Không tìm thấy thông tin người chơi.");
            }

            var item = await _unitOfWork.Repository<HrkPlayerInventory>().Query()
                .FirstOrDefaultAsync(i => i.Id == inventoryItemId && i.PlayerId == player.Id && i.IsActive, cancellationToken);

            if (item == null)
            {
                throw new KeyNotFoundException("Không tìm thấy vật phẩm trong hành trang.");
            }

            item.IsLocked = isLocked;
            item.UpdatedOn = DateTime.UtcNow;
            _unitOfWork.Repository<HrkPlayerInventory>().Update(item);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return item.IsLocked;
        }

        public async Task<PlayerWalletDto> ExpandCapacityAsync(string userId, int slotsToAdd, CancellationToken cancellationToken = default)
        {
            if (slotsToAdd <= 0 || slotsToAdd % _settings.SlotStepIncrement != 0)
            {
                throw new ArgumentException($"Số ô mở rộng phải là bội số của {_settings.SlotStepIncrement} và lớn hơn 0.");
            }

            var player = await _gamePlayerService.GetPlayerByUserIdAsync(userId, cancellationToken);
            if (player == null)
            {
                throw new KeyNotFoundException("Không tìm thấy thông tin người chơi.");
            }

            var wallet = await _gamePlayerService.GetWalletByPlayerIdAsync(player.Id, cancellationToken);
            if (wallet == null)
            {
                throw new KeyNotFoundException("Không tìm thấy ví người chơi.");
            }

            if (wallet.MaxCapacity + slotsToAdd > _settings.MaxBagCapacity)
            {
                throw new InvalidOperationException($"Hành trang đã đạt giới hạn tối đa cho phép ({_settings.MaxBagCapacity} ô).");
            }

            int diamondCost = slotsToAdd * _settings.DiamondCostPerSlot;
            if (wallet.Diamonds < diamondCost)
            {
                throw new InvalidOperationException($"Không đủ Kim Cương để mở rộng hành trang. Cần {diamondCost} Kim Cương (hiện có {wallet.Diamonds}).");
            }

            wallet.Diamonds -= diamondCost;
            wallet.MaxCapacity += slotsToAdd;
            wallet.UpdatedOn = DateTime.UtcNow;

            _unitOfWork.Repository<HrkPlayerWallet>().Update(wallet);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new PlayerWalletDto
            {
                PlayerId = wallet.PlayerId,
                Gold = wallet.Gold,
                Diamonds = wallet.Diamonds,
                UpgradeMaterials = wallet.UpgradeMaterials,
                MaxCapacity = wallet.MaxCapacity,
                UpdatedOn = wallet.UpdatedOn
            };
        }

        public async Task<HrkPlayerInventory> AddItemToInventoryAsync(string userId, int itemTemplateId, int count = 1, CancellationToken cancellationToken = default)
        {
            if (count <= 0) count = 1;

            var player = await _gamePlayerService.GetPlayerByUserIdAsync(userId, cancellationToken);
            if (player == null)
            {
                throw new KeyNotFoundException("Không tìm thấy thông tin người chơi.");
            }

            var template = await _unitOfWork.ReadOnlyRepository<HrkItemTemplate>().Query()
                .Include(t => t.Category)
                .Include(t => t.Attributes).ThenInclude(a => a.AttributeType)
                .FirstOrDefaultAsync(t => t.Id == itemTemplateId, cancellationToken);

            if (template == null)
            {
                throw new KeyNotFoundException($"Không tìm thấy vật phẩm mẫu có ID {itemTemplateId}.");
            }

            var wallet = await _gamePlayerService.GetWalletByPlayerIdAsync(player.Id, cancellationToken);
            int currentItemsCount = await _unitOfWork.ReadOnlyRepository<HrkPlayerInventory>().Query()
                .CountAsync(i => i.PlayerId == player.Id && i.IsActive && !i.IsEquipped, cancellationToken);

            int bagLimit = wallet?.MaxCapacity ?? 200;

            // Quy tắc 8: Trang bị có Count = 1, mỗi món 1 dòng riêng. Vật phẩm stackable gom nhóm Count > 1
            if (template.IsStackable)
            {
                var existingItem = await _unitOfWork.Repository<HrkPlayerInventory>().Query()
                    .FirstOrDefaultAsync(i => i.PlayerId == player.Id && i.ItemTemplateId == itemTemplateId && i.IsActive, cancellationToken);

                if (existingItem != null)
                {
                    existingItem.Count += count;
                    existingItem.UpdatedOn = DateTime.UtcNow;
                    _unitOfWork.Repository<HrkPlayerInventory>().Update(existingItem);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                    return existingItem;
                }
            }

            // Nếu tạo dòng mới, kiểm tra sức chứa túi
            if (currentItemsCount >= bagLimit)
            {
                throw new InvalidOperationException($"Hành trang đã đầy ({currentItemsCount}/{bagLimit}). Vui lòng dọn dẹp hoặc mở rộng túi.");
            }

            // Đối với trang bị: Count = 1, tính toán CurrentStats JSON ban đầu
            string? currentStatsJson = null;
            if (template.Category?.IsEquipment == true || !template.IsStackable)
            {
                count = 1;
                currentStatsJson = _statCalculationService.CalculateCurrentStatsJson(template, 0, 0);
            }

            var newInventoryItem = new HrkPlayerInventory
            {
                PlayerId = player.Id,
                ItemTemplateId = template.Id,
                Count = count,
                Enhancement = 0,
                Stars = 0,
                IsEquipped = false,
                EquippedHeroId = null,
                IsLocked = false,
                IsActive = true,
                CurrentStats = currentStatsJson,
                AcquiredOn = DateTime.UtcNow,
                UpdatedOn = DateTime.UtcNow
            };

            await _unitOfWork.Repository<HrkPlayerInventory>().AddAsync(newInventoryItem);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return newInventoryItem;
        }
    }
}
