using Core.Common.Repositories;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;
using GAME.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace GAME.Infrastructure.Services
{
    public class HeroEquipmentService : IHeroEquipmentService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IGamePlayerService _gamePlayerService;

        private static readonly HashSet<string> ValidSlotCodes = new(StringComparer.OrdinalIgnoreCase)
        {
            "WEAPON", "ARMOR", "HELMET", "BOOTS", "RING", "ARTIFACT"
        };

        public HeroEquipmentService(IUnitOfWork unitOfWork, IGamePlayerService gamePlayerService)
        {
            _unitOfWork = unitOfWork;
            _gamePlayerService = gamePlayerService;
        }

        public async Task<PlayerHeroDetailDto> EquipHeroItemAsync(
            string userId,
            long heroId,
            long inventoryItemId,
            CancellationToken cancellationToken = default)
        {
            var player = await _gamePlayerService.GetPlayerByUserIdAsync(userId, cancellationToken)
                ?? throw new KeyNotFoundException("Không tìm thấy thông tin người chơi.");

            var hero = await _unitOfWork.Repository<HrkPlayerHero>().Query()
                .FirstOrDefaultAsync(h => h.Id == heroId && h.PlayerId == player.Id && h.IsActive, cancellationToken)
                ?? throw new KeyNotFoundException("Không tìm thấy võ tướng hoặc võ tướng không thuộc tài khoản này.");

            var item = await _unitOfWork.Repository<HrkPlayerInventory>().Query()
                .Include(i => i.ItemTemplate).ThenInclude(t => t.Category)
                .FirstOrDefaultAsync(i => i.Id == inventoryItemId && i.PlayerId == player.Id && i.IsActive, cancellationToken)
                ?? throw new KeyNotFoundException("Không tìm thấy trang bị trong hành trang của người chơi.");

            if (item.ItemTemplate?.Category == null || !item.ItemTemplate.Category.IsEquipment)
            {
                throw new InvalidOperationException("Vật phẩm này không phải là trang bị có thể mặc.");
            }

            if (item.IsLocked)
            {
                throw new InvalidOperationException("Trang bị đang bị khóa, vui lòng mở khóa trước khi mặc.");
            }

            if (item.IsEquipped && item.EquippedHeroId.HasValue && item.EquippedHeroId.Value != heroId)
            {
                throw new InvalidOperationException($"Trang bị đang được mặc bởi võ tướng khác (ID: {item.EquippedHeroId.Value}). Vui lòng tháo trước khi chuyển.");
            }

            var slotCode = (item.ItemTemplate.Category.Code ?? "").Trim().ToUpperInvariant();
            if (!ValidSlotCodes.Contains(slotCode))
            {
                throw new InvalidOperationException($"Danh mục trang bị '{slotCode}' không khớp với 6 vị trí trang bị trên võ tướng.");
            }

            await _unitOfWork.ExecuteStrategyAsync(async () =>
            {
                await _unitOfWork.BeginTransactionAsync();
                try
                {
                    var equipment = await _unitOfWork.Repository<HrkPlayerEquipment>().Query()
                        .FirstOrDefaultAsync(e => e.PlayerId == player.Id && e.HeroId == heroId, cancellationToken);

                    var isNewEquipmentRecord = equipment == null;

                    if (isNewEquipmentRecord)
                    {
                        equipment = new HrkPlayerEquipment
                        {
                            PlayerId = player.Id,
                            HeroId = heroId
                        };
                        await _unitOfWork.Repository<HrkPlayerEquipment>().AddAsync(equipment);
                    }

                    long? oldItemId = GetSlotItemId(equipment!, slotCode);
                    if (oldItemId.HasValue && oldItemId.Value != item.Id)
                    {
                        var oldItem = await _unitOfWork.Repository<HrkPlayerInventory>().Query()
                            .FirstOrDefaultAsync(i => i.Id == oldItemId.Value && i.PlayerId == player.Id, cancellationToken);

                        if (oldItem != null)
                        {
                            oldItem.IsEquipped = false;
                            oldItem.EquippedHeroId = null;
                            oldItem.UpdatedOn = DateTime.UtcNow;
                            _unitOfWork.Repository<HrkPlayerInventory>().Update(oldItem);
                        }
                    }

                    SetSlotItemId(equipment!, slotCode, item.Id);
                    // AddAsync keeps EF state as Added. Calling Update immediately after AddAsync
                    // changes that state to Modified and can make the first equip attempt fail.
                    if (!isNewEquipmentRecord)
                    {
                        _unitOfWork.Repository<HrkPlayerEquipment>().Update(equipment!);
                    }

                    item.IsEquipped = true;
                    item.EquippedHeroId = heroId;
                    item.UpdatedOn = DateTime.UtcNow;
                    _unitOfWork.Repository<HrkPlayerInventory>().Update(item);

                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                    await _unitOfWork.CommitTransactionAsync();
                }
                catch
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    throw;
                }
            });
            var updatedDetail = await _gamePlayerService.GetPlayerHeroDetailAsync(userId, heroId, cancellationToken);
            return updatedDetail ?? throw new InvalidOperationException("Không thể lấy dữ liệu võ tướng sau khi trang bị.");
        }

        public async Task<PlayerHeroDetailDto> UnequipHeroItemAsync(
            string userId,
            long heroId,
            string slotCode,
            CancellationToken cancellationToken = default)
        {
            var cleanSlot = (slotCode ?? "").Trim().ToUpperInvariant();
            if (!ValidSlotCodes.Contains(cleanSlot))
            {
                throw new ArgumentException($"Vị trí trang bị '{slotCode}' không hợp lệ. Cho phép: {string.Join(", ", ValidSlotCodes)}.");
            }

            var player = await _gamePlayerService.GetPlayerByUserIdAsync(userId, cancellationToken)
                ?? throw new KeyNotFoundException("Không tìm thấy thông tin người chơi.");

            var hero = await _unitOfWork.Repository<HrkPlayerHero>().Query()
                .FirstOrDefaultAsync(h => h.Id == heroId && h.PlayerId == player.Id && h.IsActive, cancellationToken)
                ?? throw new KeyNotFoundException("Không tìm thấy võ tướng hoặc võ tướng không thuộc tài khoản này.");

            var equipment = await _unitOfWork.Repository<HrkPlayerEquipment>().Query()
                .FirstOrDefaultAsync(e => e.PlayerId == player.Id && e.HeroId == heroId, cancellationToken);

            if (equipment == null)
            {
                var detailWithoutEq = await _gamePlayerService.GetPlayerHeroDetailAsync(userId, heroId, cancellationToken);
                return detailWithoutEq ?? throw new InvalidOperationException("Không thể lấy dữ liệu võ tướng.");
            }

            long? currentItemId = GetSlotItemId(equipment, cleanSlot);
            if (!currentItemId.HasValue)
            {
                var currentDetail = await _gamePlayerService.GetPlayerHeroDetailAsync(userId, heroId, cancellationToken);
                return currentDetail ?? throw new InvalidOperationException("Không thể lấy dữ liệu võ tướng.");
            }

            var wallet = await _gamePlayerService.GetWalletByPlayerIdAsync(player.Id, cancellationToken);
            int currentBagItemsCount = await _unitOfWork.ReadOnlyRepository<HrkPlayerInventory>().Query()
                .CountAsync(i => i.PlayerId == player.Id && i.IsActive && !i.IsEquipped, cancellationToken);
            int bagLimit = wallet?.MaxCapacity ?? 200;
            if (currentBagItemsCount + 1 > bagLimit)
            {
                throw new InvalidOperationException($"Hành trang đã đầy ({currentBagItemsCount}/{bagLimit} ô). Vui lòng dọn bớt hoặc mở rộng túi đồ trước khi tháo.");
            }



            await _unitOfWork.ExecuteStrategyAsync(async () =>
            {
                await _unitOfWork.BeginTransactionAsync();
                try
                {
                    SetSlotItemId(equipment, cleanSlot, null);
                    _unitOfWork.Repository<HrkPlayerEquipment>().Update(equipment);

                    var item = await _unitOfWork.Repository<HrkPlayerInventory>().Query()
                        .FirstOrDefaultAsync(i => i.Id == currentItemId.Value && i.PlayerId == player.Id, cancellationToken);

                    if (item != null)
                    {
                        item.IsEquipped = false;
                        item.EquippedHeroId = null;
                        item.UpdatedOn = DateTime.UtcNow;
                        _unitOfWork.Repository<HrkPlayerInventory>().Update(item);
                    }

                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                    await _unitOfWork.CommitTransactionAsync();
                }
                catch
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    throw;
                }
            });

           
            var updatedDetail = await _gamePlayerService.GetPlayerHeroDetailAsync(userId, heroId, cancellationToken);
            return updatedDetail ?? throw new InvalidOperationException("Không thể lấy dữ liệu võ tướng sau khi tháo trang bị.");
        }

        public async Task<PlayerHeroDetailDto> UnequipAllHeroItemsAsync(
            string userId,
            long heroId,
            CancellationToken cancellationToken = default)
        {
            var player = await _gamePlayerService.GetPlayerByUserIdAsync(userId, cancellationToken)
                ?? throw new KeyNotFoundException("Không tìm thấy thông tin người chơi.");

            var hero = await _unitOfWork.Repository<HrkPlayerHero>().Query()
                .FirstOrDefaultAsync(h => h.Id == heroId && h.PlayerId == player.Id && h.IsActive, cancellationToken)
                ?? throw new KeyNotFoundException("Không tìm thấy võ tướng hoặc võ tướng không thuộc tài khoản này.");

            var equipment = await _unitOfWork.Repository<HrkPlayerEquipment>().Query()
                .FirstOrDefaultAsync(e => e.PlayerId == player.Id && e.HeroId == heroId, cancellationToken);

            if (equipment == null)
            {
                var detailWithoutEq = await _gamePlayerService.GetPlayerHeroDetailAsync(userId, heroId, cancellationToken);
                return detailWithoutEq ?? throw new InvalidOperationException("Không thể lấy dữ liệu võ tướng.");
            }

            var equippedItemIds = new List<long>();
            if (equipment.WeaponId.HasValue) equippedItemIds.Add(equipment.WeaponId.Value);
            if (equipment.ArmorId.HasValue) equippedItemIds.Add(equipment.ArmorId.Value);
            if (equipment.HelmetId.HasValue) equippedItemIds.Add(equipment.HelmetId.Value);
            if (equipment.BootsId.HasValue) equippedItemIds.Add(equipment.BootsId.Value);
            if (equipment.RingId.HasValue) equippedItemIds.Add(equipment.RingId.Value);
            if (equipment.ArtifactId.HasValue) equippedItemIds.Add(equipment.ArtifactId.Value);

            if (equippedItemIds.Count == 0)
            {
                var currentDetail = await _gamePlayerService.GetPlayerHeroDetailAsync(userId, heroId, cancellationToken);
                return currentDetail ?? throw new InvalidOperationException("Không thể lấy dữ liệu võ tướng.");
            }

            var wallet = await _gamePlayerService.GetWalletByPlayerIdAsync(player.Id, cancellationToken);
            int currentBagItemsCount = await _unitOfWork.ReadOnlyRepository<HrkPlayerInventory>().Query()
                .CountAsync(i => i.PlayerId == player.Id && i.IsActive && !i.IsEquipped, cancellationToken);
            int bagLimit = wallet?.MaxCapacity ?? 200;
            if (currentBagItemsCount + equippedItemIds.Count > bagLimit)
            {
                throw new InvalidOperationException($"Hành trang không đủ chỗ để tháo {equippedItemIds.Count} trang bị ({currentBagItemsCount}/{bagLimit} ô). Vui lòng dọn bớt hoặc mở rộng túi đồ.");
            }

            await _unitOfWork.ExecuteStrategyAsync(async () =>
            {
                await _unitOfWork.BeginTransactionAsync();
                try
                {
                    equipment.WeaponId = null;
                    equipment.ArmorId = null;
                    equipment.HelmetId = null;
                    equipment.BootsId = null;
                    equipment.RingId = null;
                    equipment.ArtifactId = null;
                    _unitOfWork.Repository<HrkPlayerEquipment>().Update(equipment);

                    var items = await _unitOfWork.Repository<HrkPlayerInventory>().Query()
                        .Where(i => equippedItemIds.Contains(i.Id) && i.PlayerId == player.Id)
                        .ToListAsync(cancellationToken);

                    foreach (var item in items)
                    {
                        item.IsEquipped = false;
                        item.EquippedHeroId = null;
                        item.UpdatedOn = DateTime.UtcNow;
                        _unitOfWork.Repository<HrkPlayerInventory>().Update(item);
                    }

                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                    await _unitOfWork.CommitTransactionAsync();
                }
                catch
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    throw;
                }
            });

            var updated = await _gamePlayerService.GetPlayerHeroDetailAsync(userId, heroId, cancellationToken);
            return updated ?? throw new InvalidOperationException("Không thể lấy dữ liệu võ tướng sau khi tháo toàn bộ trang bị.");
        }

        public async Task<SwapHeroEquipmentResultDto> SwapHeroEquipmentAsync(
            string userId,
            long sourceHeroId,
            long targetHeroId,
            CancellationToken cancellationToken = default)
        {
            if (sourceHeroId == targetHeroId)
            {
                throw new ArgumentException("Hai võ tướng hoán đổi phải khác nhau.");
            }

            var player = await _gamePlayerService.GetPlayerByUserIdAsync(userId, cancellationToken)
                ?? throw new KeyNotFoundException("Không tìm thấy thông tin người chơi.");

            var sourceHero = await _unitOfWork.Repository<HrkPlayerHero>().Query()
                .FirstOrDefaultAsync(h => h.Id == sourceHeroId && h.PlayerId == player.Id && h.IsActive, cancellationToken)
                ?? throw new KeyNotFoundException($"Không tìm thấy võ tướng nguồn (ID: {sourceHeroId}) hoặc không thuộc quyền sở hữu.");

            var targetHero = await _unitOfWork.Repository<HrkPlayerHero>().Query()
                .FirstOrDefaultAsync(h => h.Id == targetHeroId && h.PlayerId == player.Id && h.IsActive, cancellationToken)
                ?? throw new KeyNotFoundException($"Không tìm thấy võ tướng đích (ID: {targetHeroId}) hoặc không thuộc quyền sở hữu.");

            await _unitOfWork.ExecuteStrategyAsync(async () =>
            {
                await _unitOfWork.BeginTransactionAsync();
                try
                {
                    var sourceEquipment = await _unitOfWork.Repository<HrkPlayerEquipment>().Query()
                        .FirstOrDefaultAsync(e => e.PlayerId == player.Id && e.HeroId == sourceHeroId, cancellationToken);

                    var targetEquipment = await _unitOfWork.Repository<HrkPlayerEquipment>().Query()
                        .FirstOrDefaultAsync(e => e.PlayerId == player.Id && e.HeroId == targetHeroId, cancellationToken);

                    bool isNewSource = sourceEquipment == null;
                    if (isNewSource)
                    {
                        sourceEquipment = new HrkPlayerEquipment { PlayerId = player.Id, HeroId = sourceHeroId };
                        await _unitOfWork.Repository<HrkPlayerEquipment>().AddAsync(sourceEquipment);
                    }

                    bool isNewTarget = targetEquipment == null;
                    if (isNewTarget)
                    {
                        targetEquipment = new HrkPlayerEquipment { PlayerId = player.Id, HeroId = targetHeroId };
                        await _unitOfWork.Repository<HrkPlayerEquipment>().AddAsync(targetEquipment);
                    }

                    var srcWeapon = sourceEquipment!.WeaponId;
                    var srcArmor = sourceEquipment.ArmorId;
                    var srcHelmet = sourceEquipment.HelmetId;
                    var srcBoots = sourceEquipment.BootsId;
                    var srcRing = sourceEquipment.RingId;
                    var srcArtifact = sourceEquipment.ArtifactId;

                    var tgtWeapon = targetEquipment!.WeaponId;
                    var tgtArmor = targetEquipment.ArmorId;
                    var tgtHelmet = targetEquipment.HelmetId;
                    var tgtBoots = targetEquipment.BootsId;
                    var tgtRing = targetEquipment.RingId;
                    var tgtArtifact = targetEquipment.ArtifactId;

                    sourceEquipment.WeaponId = tgtWeapon;
                    sourceEquipment.ArmorId = tgtArmor;
                    sourceEquipment.HelmetId = tgtHelmet;
                    sourceEquipment.BootsId = tgtBoots;
                    sourceEquipment.RingId = tgtRing;
                    sourceEquipment.ArtifactId = tgtArtifact;

                    targetEquipment.WeaponId = srcWeapon;
                    targetEquipment.ArmorId = srcArmor;
                    targetEquipment.HelmetId = srcHelmet;
                    targetEquipment.BootsId = srcBoots;
                    targetEquipment.RingId = srcRing;
                    targetEquipment.ArtifactId = srcArtifact;

                    if (!isNewSource) _unitOfWork.Repository<HrkPlayerEquipment>().Update(sourceEquipment);
                    if (!isNewTarget) _unitOfWork.Repository<HrkPlayerEquipment>().Update(targetEquipment);

                    var itemsMovingToTarget = new List<long?> { srcWeapon, srcArmor, srcHelmet, srcBoots, srcRing, srcArtifact }
                        .Where(x => x.HasValue).Select(x => x!.Value).ToList();

                    var itemsMovingToSource = new List<long?> { tgtWeapon, tgtArmor, tgtHelmet, tgtBoots, tgtRing, tgtArtifact }
                        .Where(x => x.HasValue).Select(x => x!.Value).ToList();

                    if (itemsMovingToTarget.Count > 0)
                    {
                        var targetMovedItems = await _unitOfWork.Repository<HrkPlayerInventory>().Query()
                            .Where(i => itemsMovingToTarget.Contains(i.Id) && i.PlayerId == player.Id)
                            .ToListAsync(cancellationToken);

                        foreach (var itm in targetMovedItems)
                        {
                            itm.EquippedHeroId = targetHeroId;
                            itm.IsEquipped = true;
                            itm.UpdatedOn = DateTime.UtcNow;
                            _unitOfWork.Repository<HrkPlayerInventory>().Update(itm);
                        }
                    }

                    if (itemsMovingToSource.Count > 0)
                    {
                        var sourceMovedItems = await _unitOfWork.Repository<HrkPlayerInventory>().Query()
                            .Where(i => itemsMovingToSource.Contains(i.Id) && i.PlayerId == player.Id)
                            .ToListAsync(cancellationToken);

                        foreach (var itm in sourceMovedItems)
                        {
                            itm.EquippedHeroId = sourceHeroId;
                            itm.IsEquipped = true;
                            itm.UpdatedOn = DateTime.UtcNow;
                            _unitOfWork.Repository<HrkPlayerInventory>().Update(itm);
                        }
                    }

                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                    await _unitOfWork.CommitTransactionAsync();
                }
                catch
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    throw;
                }
            });

            var sourceDetail = await _gamePlayerService.GetPlayerHeroDetailAsync(userId, sourceHeroId, cancellationToken);
            var targetDetail = await _gamePlayerService.GetPlayerHeroDetailAsync(userId, targetHeroId, cancellationToken);

            return new SwapHeroEquipmentResultDto
            {
                SourceHero = sourceDetail ?? throw new InvalidOperationException("Không thể lấy dữ liệu võ tướng nguồn sau khi hoán đổi."),
                TargetHero = targetDetail ?? throw new InvalidOperationException("Không thể lấy dữ liệu võ tướng đích sau khi hoán đổi.")
            };
        }


        private static long? GetSlotItemId(HrkPlayerEquipment equipment, string slotCode)
        {
            return slotCode switch
            {
                "WEAPON" => equipment.WeaponId,
                "ARMOR" => equipment.ArmorId,
                "HELMET" => equipment.HelmetId,
                "BOOTS" => equipment.BootsId,
                "RING" => equipment.RingId,
                "ARTIFACT" => equipment.ArtifactId,
                _ => null
            };
        }

        private static void SetSlotItemId(HrkPlayerEquipment equipment, string slotCode, long? itemId)
        {
            switch (slotCode)
            {
                case "WEAPON": equipment.WeaponId = itemId; break;
                case "ARMOR": equipment.ArmorId = itemId; break;
                case "HELMET": equipment.HelmetId = itemId; break;
                case "BOOTS": equipment.BootsId = itemId; break;
                case "RING": equipment.RingId = itemId; break;
                case "ARTIFACT": equipment.ArtifactId = itemId; break;
            }
        }
    }
}
