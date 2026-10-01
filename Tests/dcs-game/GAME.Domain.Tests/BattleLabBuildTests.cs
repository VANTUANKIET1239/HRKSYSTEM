using GAME.Application.DTOs;
using GAME.Application.Interfaces;
using GAME.Domain.Entities;
using GAME.Infrastructure.Services;
using Xunit;

namespace GAME.Domain.Tests;

public sealed class BattleLabBuildTests
{
    private static (BattleLabBuildResolver resolver, TowerTestStore store) Setup()
    {
        var store = new TowerTestStore();
        store.Data<HrkHeroTemplate>().Add(new() { Id = 1, Name = "Test", RarityId = 1, BaseHp = 1000, BaseAtk = 100, BaseDef = 100, BaseSpd = 100 });
        store.Data<HrkHeroRarityUpgradeConfig>().Add(new() { RarityId = 1, StatGrowthRate = .05m, MaxLevel = 100 });
        store.Data<HrkHeroStarUpgradeConfig>().Add(new() { RarityId = 1, NextStar = 2, GrowthBonusPercent = .2m, ExtraAttributeRollCount = 1 });
        var atk = new HrkAttributeType { Id = 1, Code = "ATK", Name = "Attack" };
        store.Data<HrkAttributeType>().Add(atk);
        store.Data<HrkAttributeType>().Add(new() { Id = 2, Code = "CRIT_RATE", Name = "Crit", IsPercentage = true });
        store.Data<HrkHeroStarAttributePool>().Add(new() { Id = 1, RarityId = 1, AttributeTypeId = 2, MinValue = 2, MaxValue = 2, Weight = 1 });
        store.Data<HrkEquipmentRarityRollConfig>().Add(new() { RarityId = 1, EnhancementGrowthMinPercent = 10, EnhancementGrowthMaxPercent = 10 });
        store.Data<HrkItemTemplate>().Add(new()
        {
            Id = 10, Name = "Sword", Code = "SWORD", RarityId = 1, CategoryId = 1,
            Category = new() { Id = 1, Code = "Weapon", Name = "Weapon", IsEquipment = true },
            Attributes = [new() { AttributeTypeId = 1, AttributeType = atk, Value = 20, MinValue = 20, MaxValue = 20 }]
        });
        var itemStats = new ItemStatCalculationService();
        return (new BattleLabBuildResolver(store, new HeroProgressionStatService(),
            new HeroStatCalculationService(itemStats), itemStats, new Power()), store);
    }

    private static PlayerHeroDto Hero() => new() { Id = 1, HeroTemplateId = 1, Position = 1, Name = "Test" };
    private static BattleLabBuildDto Build() => new()
    {
        Level = 11, Stars = 2, AuraTier = 4, RollSeed = 42,
        Equipment = [new() { ItemTemplateId = 10, Enhancement = 5, Stars = 0 }]
    };

    [Fact]
    public async Task Uses_real_progression_and_equipment_once_without_writes()
    {
        var (resolver, store) = Setup();
        var hero = Hero();
        var result = await resolver.ResolveAsync(hero, Build(), 0, default);
        // 100 ATK * (1 + 10 * .05 * 1.2) = 160; sword = 20 * 1.5 = 30.
        Assert.Equal(190, hero.Stats.Atk);
        Assert.Equal(1600, hero.Stats.Hp);
        Assert.Equal(7, hero.Stats.Crit);
        Assert.Equal(30, result.Equipment[0].CurrentStats["ATK"]);
        Assert.Equal(30, result.Equipment[0].RolledAttributes[0].CurrentValue);
        Assert.Single(result.StarBonuses);
        Assert.Empty(store.Data<HrkPlayerHero>());
        Assert.Empty(store.Data<HrkPlayerInventory>());
        var again = Hero();
        await resolver.ResolveAsync(again, Build(), 1, default);
        Assert.Equal(hero.Stats.Atk, again.Stats.Atk);
    }

    [Fact]
    public async Task Rejects_duplicate_slots_and_level_above_config()
    {
        var (resolver, _) = Setup();
        var build = Build(); build.Equipment.Add(build.Equipment[0]);
        await Assert.ThrowsAsync<ArgumentException>(() => resolver.ResolveAsync(Hero(), build, 0, default));
        build = Build(); build.Level = 101;
        await Assert.ThrowsAsync<ArgumentException>(() => resolver.ResolveAsync(Hero(), build, 0, default));
    }

    [Fact]
    public void Star_roll_is_repeatable_and_within_pool_bounds()
    {
        var pool = new[] { new HrkHeroStarAttributePool { Weight = 1, MinValue = 1, MaxValue = 5 } };
        var seed = Guid.Parse("11111111-2222-3333-4444-555555555555");
        Assert.Equal(HeroStarRollCalculator.Roll(pool, seed, 1), HeroStarRollCalculator.Roll(pool, seed, 1));
        Assert.InRange(HeroStarRollCalculator.Roll(pool, seed, 1).Value, 1m, 5m);
    }

    private sealed class Power : ICombatPowerService
    {
        public Task<List<CombatPowerConfigDto>> GetConfigsAsync(CancellationToken ct = default) => Task.FromResult(new List<CombatPowerConfigDto>());
        public int Calculate(CalculatedStatsDto stats, IReadOnlyCollection<CombatPowerConfigDto> configs) => stats.Atk;
        public Task<int> CalculateAsync(CalculatedStatsDto stats, CancellationToken ct = default) => Task.FromResult(stats.Atk);
    }
}
