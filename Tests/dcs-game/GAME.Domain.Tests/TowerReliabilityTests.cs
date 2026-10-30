using Core.Common.Errors;
using GAME.Application.Common;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;
using GAME.Domain.Entities;
using GAME.Application.Services;
using Xunit;

namespace GAME.Domain.Tests;

public partial class TowerReliabilityTests
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
        return (store, new TowerRewardService(store, TestGamePlayers.Create(store), factory, new Levels(), new HeroProgressionStatService(), new Power()), factory);
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
        store.Data<HrkHeroTemplate>().Add(new() { Id = 3, Name = "Hero", Avatar = "" });
        store.Data<HrkPlayerHero>().Add(new() { Id = 7, PlayerId = 1, HeroTemplateId = 3, Level = 1 });
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
        Assert.Equal("EVENT_CLOSED", Assert.Throws<BusinessException>(() => EventAccessPolicy.EnsureCanEnter(ev, 10, now)).Code);
        ev.StartTimeUtc = now.AddHours(-1);
        var level = Assert.Throws<BusinessException>(() => EventAccessPolicy.EnsureCanEnter(ev, 9, now));
        Assert.Equal("PLAYER_LEVEL_REQUIRED", level.Code);
        Assert.Equal("Cần đạt cấp {{p0}} để tham gia.", level.MessageTemplate);
        Assert.Equal(10, level.Arguments!["p0"]);
        ev.MaxDailyRuns = 2;
        Assert.Equal("DAILY_RUN_LIMIT", Assert.Throws<BusinessException>(() => EventAccessPolicy.EnsureNewRun(ev,
            new() { RemainingLives = 0, CurrentRunNumber = 2 })).Code);
    }

    [Fact]
    public void InvalidServerEventConfiguration_RemainsAnInternalFailure()
    {
        var ev = new HrkGameEvent { IsOpen = true, MinPlayerLevel = 0, InitialLives = 0 };
        Assert.Throws<InvalidOperationException>(() => EventAccessPolicy.EnsureCanEnter(ev, 1, DateTime.UtcNow));
        ev.InitialLives = 3;
        ev.LifeConsumeMode = "INVALID";
        Assert.Throws<InvalidOperationException>(() => EventAccessPolicy.EnsureCanEnter(ev, 1, DateTime.UtcNow));
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
}
