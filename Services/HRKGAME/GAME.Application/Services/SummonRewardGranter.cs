using GAME.Application.Templates;
using Core.Common.Errors;
using GAME.Application.Common;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;
using GAME.Domain.Entities;
using Core.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace GAME.Application.Services;
/// <summary>Stages all rewards in the caller's transaction; never saves or commits independently.</summary>
public sealed class SummonRewardGranter(

    IUnitOfWork unitOfWork,

    IHeroProgressionStatService stats,

    ICombatPowerService power,

    ILevelExperienceService levels,

    ISummonRandom random

, IGameTemplateCatalog? templates = null)
{
    private readonly IGameTemplateCatalog _templates = templates ?? new DatabaseTemplateCatalog(unitOfWork);
    public async Task EnsureReadyAsync(long playerId, IReadOnlyList<HrkHeroTemplate> heroes, int count, int bagLimit, CancellationToken ct)
    {
        var heroIds = heroes.Select(x => x.Id).Distinct().ToList();
        var rarityIds = heroes.Select(x => x.RarityId).Distinct().ToList();
        var configuredRarities = (await _templates.GetAllAsync<HrkHeroRarityUpgradeConfig>(ct))
            .Where(x => rarityIds.Contains(x.RarityId))
            .Select(x => x.RarityId)
            .ToList();
        var attributes = (await _templates.GetAllAsync<HrkHeroStarAttributePool>(ct))
            .Where(x => x.IsEnabled && rarityIds.Contains(x.RarityId))
            .ToList();
        var attributeIds = (await _templates.GetAllAsync<HrkAttributeType>(ct))
            .Where(x => x.Code == "CRIT_RATE" || x.Code == "CRIT_DAMAGE" || x.Code == "LIFESTEAL" || x.Code == "ACCURACY" || x.Code == "RESISTANCE")
            .Select(x => x.Id)
            .ToList();
        if (configuredRarities.Distinct().Count() != rarityIds.Count || attributes.Select(x => x.RarityId).Distinct().Count() != rarityIds.Count || attributes.Any(x => x.Weight <= 0 || x.MinValue > x.MaxValue || !attributeIds.Contains(x.AttributeTypeId)) || attributes.GroupBy(x => x.RarityId).Any(group => group.Sum(x => (long)x.Weight) > int.MaxValue))
            throw new InvalidOperationException("Invalid hero initialization configuration in summon pool.");
        var configs = (await _templates.GetAllAsync<HrkHeroStoneConfig>(ct))
            .Where(x => heroIds.Contains(x.HeroTemplateId) && x.IsActive)
            .ToList();
        if (configs.Select(x => x.HeroTemplateId).Distinct().Count() != heroIds.Count || configs.Any(x => x.DuplicateConversionQuantity <= 0))
            throw new InvalidOperationException("Missing duplicate conversion configuration in summon pool.");
        var ids = configs.Select(x => x.ItemTemplateId).ToList();
        var templates = (await _templates.GetAllAsync<HrkItemTemplate>(ct))
            .Where(x => ids.Contains(x.Id))
            .ToDictionary(x => x.Id);
        var inventory = await unitOfWork.ReadOnlyRepository<HrkPlayerInventory>().Query()
            .Where(x => x.PlayerId == playerId && x.IsActive && !x.IsEquipped)
            .ToListAsync(ct);
        var capacities = configs.GroupBy(x => x.ItemTemplateId)
            .Select(group =>
        {
            if (!templates.TryGetValue(group.Key, out var item) || !item.IsStackable || item.MaxStackSize <= 0)
                throw new InvalidOperationException("Invalid duplicate conversion item in summon pool.");
            return new SummonStoneCapacity(group.Max(x => x.DuplicateConversionQuantity), item.MaxStackSize, inventory.Where(x => x.ItemTemplateId == item.Id).Sum(x => (long)Math.Max(0, item.MaxStackSize - x.Count)));
        }).ToList();
        var required = SummonInventoryPolicy.RequiredFreeSlots(count, capacities);
        if (inventory.Count + required > bagLimit)
            throw BusinessException.Localized("INVENTORY_FULL", $"Hãy để trống ít nhất {required} ô hành trang để nhận đủ phần thưởng chiêu mộ.", BusinessErrorKind.Conflict);
    }

    public async Task<IReadOnlyList<SummonRewardDto>> GrantAsync(long playerId, IReadOnlyList<HrkHeroTemplate> heroes, Guid requestId, int bagLimit, CancellationToken ct)
    {
        var owned = (await unitOfWork.ReadOnlyRepository<HrkPlayerHero>().Query()
            .Where(x => x.PlayerId == playerId && x.IsActive)
            .Select(x => x.HeroTemplateId)
            .ToListAsync(ct)).ToHashSet();
        var inventory = await unitOfWork.Repository<HrkPlayerInventory>().Query()
            .Where(x => x.PlayerId == playerId && x.IsActive)
            .ToListAsync(ct);
        var ids = heroes.Select(x => x.Id).Distinct().ToList();
        var stones = (await _templates.GetAllAsync<HrkHeroStoneConfig>(ct))
            .Where(x => ids.Contains(x.HeroTemplateId) && x.IsActive)
            .ToDictionary(x => x.HeroTemplateId);
        var itemIds = stones.Values.Select(x => x.ItemTemplateId).ToList();
        var stoneItems = (await _templates.GetAllAsync<HrkItemTemplate>(ct))
            .Where(x => itemIds.Contains(x.Id))
            .ToDictionary(x => x.Id);
        var heroFactory = new NewPlayerHeroFactory(unitOfWork, stats, power, levels, random, _templates);
        var results = new List<SummonRewardDto>();
        for (var index = 0; index < heroes.Count; index++)
        {
            var template = heroes[index];
            var isNew = owned.Add(template.Id); // Includes earlier draws in this same batch.
            HrkHeroStoneConfig? stone = null;
            HrkItemTemplate? stoneItem = null;
            if (isNew)
            {
                await heroFactory.CreateAsync(playerId, template, ct);
            }
            else
            {
                if (!stones.TryGetValue(template.Id, out stone) || stone.DuplicateConversionQuantity <= 0 || !stoneItems.TryGetValue(stone.ItemTemplateId, out stoneItem) || !stoneItem.IsStackable || stoneItem.MaxStackSize <= 0)
                    throw new InvalidOperationException("Missing or invalid duplicate conversion configuration.");
                await AddStonesAsync(playerId, stoneItem, stone.DuplicateConversionQuantity, inventory, bagLimit);
            }

            await unitOfWork.Repository<HrkHeroAcquisitionHistory>().AddAsync(new HrkHeroAcquisitionHistory { RequestId = Guid.NewGuid(), PlayerId = playerId, HeroTemplateId = template.Id, SourceType = "SUMMON", SourceReferenceId = $"{requestId:D}:{index}", WasDuplicate = !isNew, ConvertedHeroStoneQuantity = stone?.DuplicateConversionQuantity ?? 0 });
            results.Add(new(index, template.Id, template.Name, template.Avatar, template.Rarity.Code, template.Rarity.Name, template.Rarity.ColorHex, isNew, stoneItem?.Id, stoneItem?.Name, stone?.DuplicateConversionQuantity ?? 0));
        }

        return results;
    }

    private async Task AddStonesAsync(long playerId, HrkItemTemplate template, int quantity, List<HrkPlayerInventory> inventory, int bagLimit)
    {
        foreach (var stack in inventory.Where(x => x.ItemTemplateId == template.Id && !x.IsEquipped && x.Count < template.MaxStackSize))
        {
            var added = Math.Min(quantity, template.MaxStackSize - stack.Count);
            stack.Count += added;
            stack.UpdatedOn = DateTime.UtcNow;
            quantity -= added;
            if (quantity == 0)
                return;
        }

        while (quantity > 0)
        {
            if (inventory.Count(x => !x.IsEquipped) >= bagLimit)
                throw new BusinessException("INVENTORY_FULL", "Hành trang không đủ chỗ nhận đá tướng. Vui lòng dọn bớt trước khi chiêu mộ.", BusinessErrorKind.Conflict);
            var stack = new HrkPlayerInventory
            {
                PlayerId = playerId,
                ItemTemplateId = template.Id,
                Count = Math.Min(quantity, template.MaxStackSize)
            };
            quantity -= stack.Count;
            inventory.Add(stack);
            await unitOfWork.Repository<HrkPlayerInventory>().AddAsync(stack);
        }
    }
}
