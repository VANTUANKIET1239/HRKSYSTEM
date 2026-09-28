using Core.Common.Repositories;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;
using GAME.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GAME.Infrastructure.Services;

public sealed class DungeonRewardService : IDungeonRewardService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IItemStatCalculationService _statCalculationService;
    private readonly IDungeonEquipmentRewardPolicy _rewardPolicy;
    private readonly IEquipmentInstanceFactory _equipmentInstanceFactory;
    private readonly IRandomService _randomService;
    private readonly ILogger<DungeonRewardService> _logger;

    public DungeonRewardService(
        IUnitOfWork unitOfWork,
        IItemStatCalculationService statCalculationService,
        IDungeonEquipmentRewardPolicy rewardPolicy,
        IEquipmentInstanceFactory equipmentInstanceFactory,
        IRandomService randomService,
        ILogger<DungeonRewardService> logger)
    {
        _unitOfWork = unitOfWork;
        _statCalculationService = statCalculationService;
        _rewardPolicy = rewardPolicy;
        _equipmentInstanceFactory = equipmentInstanceFactory;
        _randomService = randomService;
        _logger = logger;
    }

    public async Task<List<DungeonPossibleDropDto>> GetPossibleDropsForStageAsync(int stageId, CancellationToken cancellationToken = default)
    {
        var result = await GetPossibleDropsForStagesAsync(new[] { stageId }, cancellationToken);
        return result.GetValueOrDefault(stageId, new List<DungeonPossibleDropDto>());
    }

    public async Task<Dictionary<int, List<DungeonPossibleDropDto>>> GetPossibleDropsForStagesAsync(
        IReadOnlyCollection<int> stageIds,
        CancellationToken cancellationToken = default)
    {
        var ids = stageIds.Distinct().ToList();
        if (ids.Count == 0)
        {
            return new Dictionary<int, List<DungeonPossibleDropDto>>();
        }

        var pools = await _unitOfWork.ReadOnlyRepository<HrkDungeonStageDropPool>().Query()
            .Where(x => ids.Contains(x.StageId) && x.IsActive)
            .Include(x => x.ItemTemplate).ThenInclude(t => t.Category)
            .Include(x => x.ItemTemplate).ThenInclude(t => t.Rarity)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return pools
            .GroupBy(p => p.StageId)
            .ToDictionary(
                group => group.Key,
                group => group.Select(p => new DungeonPossibleDropDto
                {
                    ItemTemplateId = p.ItemTemplateId,
                    Code = p.ItemTemplate.Code,
                    Name = p.ItemTemplate.Name,
                    ImagePath = p.ItemTemplate.ImagePath,
                    RarityCode = p.ItemTemplate.Rarity.Code,
                    RarityName = p.ItemTemplate.Rarity.Name,
                    RarityColorHex = p.ItemTemplate.Rarity.ColorHex,
                    CategoryCode = p.ItemTemplate.Category.Code,
                    CategoryName = p.ItemTemplate.Category.Name,
                    DropRatePercent = Math.Round(p.DropRate * 100m, 1)
                }).ToList());
    }

    public async Task<bool> IsBagFullAsync(long playerId, CancellationToken cancellationToken = default)
    {
        var wallet = await _unitOfWork.ReadOnlyRepository<HrkPlayerWallet>().Query()
            .SingleOrDefaultAsync(x => x.PlayerId == playerId, cancellationToken);
        int bagLimit = wallet?.MaxCapacity ?? 200;

        int currentItemsCount = await _unitOfWork.ReadOnlyRepository<HrkPlayerInventory>().Query()
            .CountAsync(i => i.PlayerId == playerId && i.IsActive && !i.IsEquipped, cancellationToken);

        return currentItemsCount >= bagLimit;
    }

    public async Task<(DungeonDroppedEquipmentDto? droppedEquipment, bool isBagFull)> RollEquipmentDropAsync(
        long playerId, int stageId, bool isFirstClear, CancellationToken cancellationToken = default)
    {
        var poolEntries = await _unitOfWork.Repository<HrkDungeonStageDropPool>().Query()
            .Where(x => x.StageId == stageId && x.IsActive && (!x.IsFirstClearOnly || isFirstClear))
            .Include(x => x.Stage).ThenInclude(s => s.DungeonMap).ThenInclude(m => m.MaxEquipmentRarity)
            .Include(x => x.ItemTemplate).ThenInclude(t => t.Category)
            .Include(x => x.ItemTemplate).ThenInclude(t => t.Rarity)
            .Include(x => x.ItemTemplate).ThenInclude(t => t.Attributes).ThenInclude(a => a.AttributeType)
            .ToListAsync(cancellationToken);

        if (poolEntries.Count == 0)
        {
            return (null, false);
        }

        // Overall chance for boss stage is the configured DropRate
        decimal overallDropRate = poolEntries.Max(e => e.DropRate);
        double roll = _randomService.NextDouble();
        if (roll >= (double)overallDropRate)
        {
            return (null, false);
        }

        // Weighted selection among items in pool
        int totalWeight = poolEntries.Sum(e => Math.Max(1, e.Weight));
        int selectedWeight = _randomService.Next(1, totalWeight + 1);
        int currentWeight = 0;
        HrkDungeonStageDropPool? selectedEntry = null;

        foreach (var entry in poolEntries)
        {
            currentWeight += Math.Max(1, entry.Weight);
            if (selectedWeight <= currentWeight)
            {
                selectedEntry = entry;
                break;
            }
        }

        selectedEntry ??= poolEntries[0];
        var itemTemplate = selectedEntry.ItemTemplate;

        if (!_rewardPolicy.CanReward(selectedEntry.Stage.DungeonMap, itemTemplate))
        {
            _logger.LogWarning("Drop rejected by reward policy: Map={MapCode}, Item={ItemCode}, Rarity={RarityCode}",
                selectedEntry.Stage.DungeonMap.Code, itemTemplate.Code, itemTemplate.Rarity?.Code);
            return (null, false);
        }

        // Check inventory capacity
        var wallet = await _unitOfWork.Repository<HrkPlayerWallet>().Query()
            .SingleOrDefaultAsync(x => x.PlayerId == playerId, cancellationToken);
        int bagLimit = wallet?.MaxCapacity ?? 200;

        int currentItemsCount = await _unitOfWork.ReadOnlyRepository<HrkPlayerInventory>().Query()
            .CountAsync(i => i.PlayerId == playerId && i.IsActive && !i.IsEquipped, cancellationToken);

        if (currentItemsCount >= bagLimit)
        {
            _logger.LogInformation("Player {PlayerId} bag full during equipment drop attempt. Items={Count}/{Limit}",
                playerId, currentItemsCount, bagLimit);
            return (null, true);
        }

        // Create equipment instance via EquipmentInstanceFactory
        var creationResult = await _equipmentInstanceFactory.CreateAsync(
            playerId,
            itemTemplate,
            new EquipmentAcquisitionContext
            {
                Source = "DUNGEON_DROP",
                StageId = stageId
            },
            cancellationToken);

        await _unitOfWork.Repository<HrkPlayerInventory>().AddAsync(creationResult.InventoryItem);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Update returned DTO with the generated entity Id
        creationResult.DroppedDto.InventoryItemId = creationResult.InventoryItem.Id;

        _logger.LogInformation("Rolled equipment drop for Player {PlayerId}: Item={ItemName} (Id={InventoryItemId}), Growth={Growth}%, CP={CP}",
            playerId, creationResult.DroppedDto.Name, creationResult.InventoryItem.Id, creationResult.DroppedDto.EnhancementGrowthPercent, creationResult.DroppedDto.CombatPower);

        return (creationResult.DroppedDto, false);
    }
}
