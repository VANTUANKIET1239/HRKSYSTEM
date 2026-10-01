using GAME.Application.Common;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;
using GAME.Domain.Entities;
using GAME.Infrastructure.Services;
using Xunit;

namespace GAME.Domain.Tests;

public class TowerReliabilityTests
{
    private static (TowerTestStore store, TowerRewardService service, Factory factory) Setup(int capacity = 1, bool equipment = false)
    {
        var store = new TowerTestStore();
        store.Data<HrkPlayerWallet>().Add(new() { PlayerId = 1, MaxCapacity = capacity });
        store.Data<HrkItemTemplate>().Add(new()
        {
            Id = 2, Code = "TEST", Name = "Test reward", IsStackable = !equipment, MaxStackSize = 10,
            Category = new() { IsEquipment = equipment }, Rarity = new() { Code = "RARE" }
        });
        store.Data<HrkHeroRarityUpgradeConfig>().Add(new() { RarityId = 0, StatGrowthRate = .05m });
        var factory = new Factory();
        return (store, new TowerRewardService(store, factory, new Levels(), new HeroProgressionStatService(), new Power()), factory);
    }

    [Fact]
    public async Task RetryingFullBag_DoesNotCreateAnotherPendingReward()
    {
        var (store, service, _) = Setup(0);
        var rewards = new List<GenericRewardItemDto> { new() { Type = "ITEM", ItemTemplateId = 2, Quantity = 2 } };
        var first = await service.GrantGenericRewardsAsync(1, rewards, 1, "Floor", default);
        Assert.Single(store.Data<HrkPlayerPendingReward>());
        var retry = await service.GrantGenericRewardsAsync(1, first.pending, 1, "Retry", default, createPending: false);
        Assert.Single(store.Data<HrkPlayerPendingReward>());
        Assert.Equal(2, Assert.Single(retry.pending).Quantity);
        Assert.Empty(retry.granted);
    }

    [Fact]
    public async Task ExistingStack_CanReceiveWhenSlotsAreFull_WithoutExceedingMaxStack()
    {
        var (store, service, _) = Setup();
        store.Data<HrkPlayerInventory>().Add(new() { PlayerId = 1, ItemTemplateId = 2, IsActive = true, Count = 8 });
        var result = await service.GrantGenericRewardsAsync(1,
            new() { new() { Type = "ITEM", ItemTemplateId = 2, Quantity = 5 } }, 1, "Floor", default);
        Assert.Equal(10, Assert.Single(store.Data<HrkPlayerInventory>()).Count);
        Assert.Equal(2, Assert.Single(result.granted).Quantity);
        Assert.Equal(3, Assert.Single(result.pending).Quantity);
    }

    [Fact]
    public async Task EquipmentReward_CreatesActualInstances_ForEachItem()
    {
        var (store, service, factory) = Setup(3, equipment: true);
        var result = await service.GrantGenericRewardsAsync(1,
            new() { new() { Type = "EQUIPMENT", ItemTemplateId = 2, Quantity = 2 } }, 1, "Floor", default);
        Assert.Equal(2, factory.Created);
        Assert.Equal(2, store.Data<HrkPlayerInventory>().Count);
        Assert.Equal(2, result.granted.Count);
        Assert.All(result.granted, r => Assert.NotNull(r.DroppedEquipment));
        Assert.Empty(result.pending);
    }

    [Fact]
    public async Task MissingTemplate_FailsRatherThanReportingAFalseGrant()
    {
        var (_, service, _) = Setup();
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.GrantGenericRewardsAsync(1,
            new() { new() { Type = "ITEM", Code = "MISSING", Quantity = 1 } }, 1, "Floor", default));
    }

    [Fact]
    public async Task ActualExperienceServicePath_LevelsPlayerAndParticipants()
    {
        var (store, service, _) = Setup();
        var player = new HrkPlayer { Id = 1, Level = 1, Exp = 0 };
        store.Data<HrkPlayerHero>().Add(new() { Id = 7, PlayerId = 1, Level = 1, HeroTemplate = new() { Name = "Hero", Avatar = "" } });
        await service.AddPlayerExpAsync(player, 125, default);
        var heroes = await service.AddParticipantHeroExp(1, new long[] { 7 }, 125, default);
        Assert.Equal(2, player.Level);
        Assert.Equal(25, player.Exp);
        Assert.Equal(2, Assert.Single(heroes).NewLevel);
        Assert.False(string.IsNullOrWhiteSpace(store.Data<HrkPlayerHero>()[0].CurrentStats));
    }

    [Theory]
    [InlineData("ON_DEFEAT", true, 3)]
    [InlineData("ON_DEFEAT", false, 2)]
    [InlineData("ON_ENTRY", true, 2)]
    public void UsesConfiguredLifePolicy(string policy, bool victory, int expected) =>
        Assert.Equal(expected, EventAccessPolicy.LivesAfter(new() { LifeConsumeMode = policy }, 3, victory));

    [Fact]
    public void EnforcesEventScheduleLevelAndRunLimit()
    {
        var now = DateTime.UtcNow;
        var ev = new HrkGameEvent { IsOpen = true, MinPlayerLevel = 10, StartTimeUtc = now.AddHours(1) };
        Assert.Throws<InvalidOperationException>(() => EventAccessPolicy.EnsureCanEnter(ev, 10, now));
        ev.StartTimeUtc = now.AddHours(-1);
        Assert.Throws<InvalidOperationException>(() => EventAccessPolicy.EnsureCanEnter(ev, 9, now));
        ev.MaxDailyRuns = 2;
        Assert.Throws<InvalidOperationException>(() => EventAccessPolicy.EnsureNewRun(ev,
            new() { RemainingLives = 0, CurrentRunNumber = 2 }));
    }

    [Fact]
    public async Task MaxFloor_ComesFromConfiguration_NotSixty()
    {
        var store = new TowerTestStore();
        store.Data<HrkGameEvent>().Add(new() { Id = 1, RulesJson = "{\"maxFloor\":3}" });
        store.Data<HrkTowerFloor>().AddRange(Enumerable.Range(1, 4).Select(n =>
            new HrkTowerFloor { EventId = 1, FloorNumber = n, IsActive = true }));
        var executor = new TowerBattleExecutor(store, null!, null!, null!);
        Assert.Equal(3, await executor.MaxFloorAsync(1, default));
        store.Data<HrkTowerFloor>().RemoveAt(1);
        await Assert.ThrowsAsync<InvalidOperationException>(() => executor.MaxFloorAsync(1, default));
    }

    [Fact]
    public void InvalidRewardJson_IsNotSilentlyConvertedToNoRewards() =>
        Assert.Throws<System.Text.Json.JsonException>(() => TowerRewardService.ParseRewards("{broken"));

    private sealed class Levels : ILevelExperienceService
    {
        public Task<LevelExperienceRequirement> GetHeroRequirementAsync(int level, CancellationToken cancellationToken = default) =>
            Task.FromResult(new LevelExperienceRequirement(level, 100, level >= 10));
        public Task<LevelExperienceRequirement> GetPlayerRequirementAsync(int level, CancellationToken cancellationToken = default) =>
            GetHeroRequirementAsync(level, cancellationToken);
    }
    private sealed class Power : ICombatPowerService
    {
        public Task<List<CombatPowerConfigDto>> GetConfigsAsync(CancellationToken cancellationToken = default) => Task.FromResult(new List<CombatPowerConfigDto>());
        public int Calculate(CalculatedStatsDto stats, IReadOnlyCollection<CombatPowerConfigDto> configs) => stats.Hp;
        public Task<int> CalculateAsync(CalculatedStatsDto stats, CancellationToken cancellationToken = default) => Task.FromResult(stats.Hp);
    }

    private sealed class Factory : IEquipmentInstanceFactory
    {
        public int Created { get; private set; }
        public Task<EquipmentInstanceCreationResult> CreateAsync(long playerId, HrkItemTemplate template,
            EquipmentAcquisitionContext context, CancellationToken cancellationToken = default)
        {
            Created++;
            return Task.FromResult(new EquipmentInstanceCreationResult
            {
                InventoryItem = new() { Id = Created, PlayerId = playerId, ItemTemplateId = template.Id, Count = 1, IsActive = true },
                DroppedDto = new() { InventoryItemId = Created, Name = template.Name }
            });
        }
    }
}
