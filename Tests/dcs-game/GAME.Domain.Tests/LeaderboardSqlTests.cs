using Core.Common.Errors;
using Core.Persistence.Repositories;
using GAME.Application.Services;
using GAME.Application.Configuration;
using GAME.Application.Features.Commands.Dungeon;
using GAME.Application.Features.Commands.Inventory.ExpandCapacity;
using GAME.Application.Interfaces;
using GAME.Application.Features.Commands.Formation;
using GAME.Application.Templates;
using GAME.Application.DTOs;
using GAME.Domain.Entities;
using GAME.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Xunit;
using Core.Application.CQRS;
using Core.Common.Entity.MyCompany.Shared.Responses;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace GAME.Domain.Tests;

public sealed partial class LeaderboardSqlTests(SummonDatabaseFixture database) : IClassFixture<SummonDatabaseFixture>
{
    [SummonSqlFact]
    public async Task RealCalculationTracksOnlySelectedFormationAndPublicQueriesInspectItsHeroes()
    {
        var player = await database.PlayerAsync();
        await using var db = database.Context();
        var templateId = await db.HeroTemplates.Select(x => x.Id).FirstAsync();
        var hero = new HrkPlayerHero { PlayerId = player.Id, HeroTemplateId = templateId, CurrentStats = System.Text.Json.JsonSerializer.Serialize(new CalculatedStatsDto { Hp = 2000 }), IsActive = true };
        var reserve = new HrkPlayerHero { PlayerId = player.Id, HeroTemplateId = templateId, CurrentStats = System.Text.Json.JsonSerializer.Serialize(new CalculatedStatsDto { Hp = 5000 }), IsActive = true };
        var stoneId = await db.ItemTemplates.Select(x => x.Id).FirstAsync();
        var code = Guid.NewGuid().ToString("N")[..8];
        var first = new HrkFormationTemplate { Code = "A_" + code, Name = "Default", MaxLevel = 1, Slots = [new() { Slot = 1 }], LevelConfigs = [new() { Level = 1, StoneItemTemplateId = stoneId, StatBonusJson = "{\"HpPercent\":10}" }] };
        var other = new HrkFormationTemplate { Code = "B_" + code, Name = "Reserve", MaxLevel = 1, Slots = [new() { Slot = 1 }], LevelConfigs = [new() { Level = 1, StoneItemTemplateId = stoneId, StatBonusJson = "{}" }] };
        db.AddRange(hero, reserve, first, other);
        if (!await db.CombatPowerConfigs.AnyAsync(x => x.StatCode == "HP")) db.CombatPowerConfigs.Add(new() { StatCode = "HP", PowerPerUnit = 1, IsEnabled = true });
        await db.SaveChangesAsync();
        db.AddRange(new HrkPlayerFormation { PlayerId = player.Id, FormationTemplateId = first.Id, Position1 = hero.Id, IsSelected = true },
            new HrkPlayerFormation { PlayerId = player.Id, FormationTemplateId = other.Id, Position1 = reserve.Id });
        await db.SaveChangesAsync();
        var uow = new UnitOfWork<GameDbContext>(db);
        var catalog = database.Templates();
        var stats = new HeroStatCalculationService(new ItemStatCalculationService(uow, catalog));
        var combat = new CombatPowerService(uow, catalog);
        var formationStats = new FormationStatService(stats, combat);
        var query = new FormationPowerQueryService(uow, stats, formationStats, combat, catalog);
        var writer = RankingTestSupport.Writer(uow, query);
        await uow.ExecuteInTransactionAsync(() => writer.RefreshPowerAsync(player.Id));
        Assert.Equal(2200, (await db.Set<HrkPlayerRankingStats>().AsNoTracking().SingleAsync(x => x.PlayerId == player.Id)).PeakFormationPower);
        var select = new SelectFormationCommandHandler(uow, TestGamePlayers.Create(uow), formationStats, combat, stats, catalog);
        var services = RankingTestSupport.PipelineServices(writer, new Actor(player.UserId));
        services.AddSingleton<IRequestHandler<SelectFormationCommand, BaseResponse<FormationDetailDto>>>(select);
        using var provider = services.BuildServiceProvider();
        var dispatcher = provider.GetRequiredService<ICqrsDispatcher>();
        await dispatcher.CommandAsync(new SelectFormationCommand(player.UserId, other.Code));
        await dispatcher.CommandAsync(new SelectFormationCommand(player.UserId, first.Code));
        var record = await db.Set<HrkPlayerRankingStats>().AsNoTracking().SingleAsync(x => x.PlayerId == player.Id);
        Assert.Equal(5000, record.PeakFormationPower);
        Assert.Equal(2200, record.CurrentFormationPower);
        var reader = new LeaderboardReader(uow, catalog, new FormationSnapshotService(uow, stats, formationStats, combat, catalog),
            new PlayerReadModels(uow, TestGamePlayers.Create(uow), stats, combat, query, catalog));
        var profile = await reader.GetProfileAsync(player.Id, default);
        Assert.Equal(hero.Id, Assert.Single(profile.Heroes).Id);
        Assert.Equal(2200, profile.CurrentFormationPower);
        Assert.Equal(hero.Id, (await reader.GetHeroAsync(player.Id, hero.Id, default)).Id);
        await Assert.ThrowsAsync<BusinessException>(() => reader.GetHeroAsync(player.Id, reserve.Id, default));
    }

    [SummonSqlFact]
    public async Task ActualPurchaseCommandsDoNotRepeatBenefitsOrChargesOnReplay()
    {
        var player = await database.PlayerAsync(diamonds: 1000);
        await using var db = database.Context();
        var uow = new UnitOfWork<GameDbContext>(db);
        var writer = RankingTestSupport.Writer(uow);
        var service = RankingTestSupport.Spending(uow);
        var players = TestGamePlayers.Create(uow);
        var stamina = new PurchaseStaminaCommandHandler(uow, players, service);
        var expand = new ExpandInventoryCapacityCommandHandler(uow, players, Options.Create(new InventorySettings()), new Actor(player.UserId), service);
        var services = RankingTestSupport.PipelineServices(writer, new Actor(player.UserId));
        services.AddSingleton<IRequestHandler<PurchaseStaminaCommand, BaseResponse<DungeonStaminaDto>>>(stamina);
        services.AddSingleton<IRequestHandler<ExpandInventoryCapacityCommand, BaseResponse<PlayerWalletDto>>>(expand);
        using var provider = services.BuildServiceProvider();
        var dispatcher = provider.GetRequiredService<ICqrsDispatcher>();
        var purchase = new PurchaseStaminaCommand(player.UserId, Guid.NewGuid());
        var first = await dispatcher.CommandAsync(purchase);
        var replay = await dispatcher.CommandAsync(purchase);
        Assert.Equal(first.Data!.Current, replay.Data!.Current);
        var expansion = new ExpandInventoryCapacityCommand(5, Guid.NewGuid());
        await dispatcher.CommandAsync(expansion);
        await dispatcher.CommandAsync(expansion);
        var wallet = await db.PlayerWallets.AsNoTracking().SingleAsync(x => x.PlayerId == player.Id);
        Assert.Equal(205, wallet.MaxCapacity);
        Assert.Equal(880, wallet.Diamonds); // 20 stamina + 5*20 capacity
        Assert.Equal(120, (await db.Set<HrkPlayerRankingStats>().AsNoTracking().SingleAsync(x => x.PlayerId == player.Id)).TotalDiamondsSpent);
        Assert.Equal(2, await db.Set<HrkPlayerDiamondSpendTransaction>().CountAsync(x => x.PlayerId == player.Id));
        var reader = new LeaderboardReader(uow, database.Templates(), null!, null!);
        Assert.NotNull((await reader.GetAsync(player.UserId, "diamonds-spent", 1, 20, default)).MyEntry);
    }

    [SummonSqlFact]
    public async Task CurrentPowerCanDecreaseButPeakAndItsTieBreakTimeCannot()
    {
        var player = await database.PlayerAsync();
        await using var db = database.Context();
        var uow = new UnitOfWork<GameDbContext>(db);
        var power = new RankingTestSupport.Power(100);
        var writer = RankingTestSupport.Writer(uow, power);
        await uow.ExecuteInTransactionAsync(() => writer.RefreshPowerAsync(player.Id));
        var original = await db.Set<HrkPlayerRankingStats>().AsNoTracking().SingleAsync(x => x.PlayerId == player.Id);
        power.Value = 70;
        await uow.ExecuteInTransactionAsync(() => writer.RefreshPowerAsync(player.Id));
        var lowered = await db.Set<HrkPlayerRankingStats>().AsNoTracking().SingleAsync(x => x.PlayerId == player.Id);
        Assert.Equal(70, lowered.CurrentFormationPower);
        Assert.Equal(100, lowered.PeakFormationPower);
        Assert.Equal(original.PeakPowerAchievedAtUtc, lowered.PeakPowerAchievedAtUtc);
        power.Value = 150;
        await uow.ExecuteInTransactionAsync(() => writer.RefreshPowerAsync(player.Id));
        Assert.Equal(150, (await db.Set<HrkPlayerRankingStats>().AsNoTracking().SingleAsync(x => x.PlayerId == player.Id)).PeakFormationPower);
    }

    [SummonSqlFact]
    public async Task SpendingIsAtomicAndTheSameReceiptNeverCountsTwice()
    {
        var player = await database.PlayerAsync(diamonds: 100);
        var id = Guid.NewGuid();
        await using var db = database.Context();
        var uow = new UnitOfWork<GameDbContext>(db);
        var writer = RankingTestSupport.Writer(uow);
        var service = RankingTestSupport.Spending(uow);
        for (var i = 0; i < 2; i++)
            await uow.ExecuteInTransactionAsync(async () =>
            {
                var wallet = await uow.Repository<HrkPlayerWallet>().Query().SingleAsync(x => x.PlayerId == player.Id);
                var spent = await service.SpendAsync(wallet, 25, "TEST", "item:1", id);
                Assert.Equal(i == 0, spent);
            });
        Assert.Equal(75, (await db.PlayerWallets.AsNoTracking().SingleAsync(x => x.PlayerId == player.Id)).Diamonds);
        Assert.Equal(25, (await db.Set<HrkPlayerRankingStats>().AsNoTracking().SingleAsync(x => x.PlayerId == player.Id)).TotalDiamondsSpent);
        Assert.Equal(1, await db.Set<HrkPlayerDiamondSpendTransaction>().CountAsync(x => x.PlayerId == player.Id));
        await Assert.ThrowsAsync<BusinessException>(() => uow.ExecuteInTransactionAsync(async () =>
        {
            var wallet = await uow.Repository<HrkPlayerWallet>().Query().SingleAsync(x => x.PlayerId == player.Id);
            await service.SpendAsync(wallet, 25, "TEST", "item:2", id);
        }));
    }

    [SummonSqlFact]
    public async Task FailedBusinessActionRollsBackWalletReceiptAndRanking()
    {
        var player = await database.PlayerAsync(diamonds: 100);
        await using var db = database.Context();
        var uow = new UnitOfWork<GameDbContext>(db);
        var service = RankingTestSupport.Spending(uow);
        await Assert.ThrowsAsync<InvalidOperationException>(() => uow.ExecuteInTransactionAsync(async () =>
        {
            var wallet = await uow.Repository<HrkPlayerWallet>().Query().SingleAsync(x => x.PlayerId == player.Id);
            await service.SpendAsync(wallet, 30, "TEST", "failed", Guid.NewGuid());
            await uow.SaveChangesAsync();
            throw new InvalidOperationException("Reward grant failed");
        }));
        Assert.Equal(100, (await db.PlayerWallets.AsNoTracking().SingleAsync(x => x.PlayerId == player.Id)).Diamonds);
        Assert.False(await db.Set<HrkPlayerDiamondSpendTransaction>().AnyAsync(x => x.PlayerId == player.Id));
        Assert.False(await db.Set<HrkPlayerRankingStats>().AnyAsync(x => x.PlayerId == player.Id));
    }

    [SummonSqlFact]
    public async Task ConcurrentRefreshesPreserveTheHigherRecord()
    {
        var player = await database.PlayerAsync();
        async Task Refresh(int value)
        {
            await using var db = database.Context();
            var uow = new UnitOfWork<GameDbContext>(db);
            await uow.ExecuteInTransactionAsync(() => RankingTestSupport.Writer(uow, value).RefreshPowerAsync(player.Id));
        }
        await Task.WhenAll(Refresh(800), Refresh(900), Refresh(850));
        await using var read = database.Context();
        Assert.Equal(900, (await read.Set<HrkPlayerRankingStats>().SingleAsync(x => x.PlayerId == player.Id)).PeakFormationPower);
    }

    [SummonSqlFact]
    public async Task PagesUseGlobalRanksAndFirstAchievementBreaksTies()
    {
        var first = await database.PlayerAsync(); var second = await database.PlayerAsync(); var third = await database.PlayerAsync();
        await using var db = database.Context();
        var uow = new UnitOfWork<GameDbContext>(db);
        foreach (var player in new[] { first, second, third })
            await uow.ExecuteInTransactionAsync(() => RankingTestSupport.Writer(uow, 1_000_000).RefreshPowerAsync(player.Id));
        await db.Set<HrkPlayerRankingStats>().Where(x => x.PlayerId == first.Id).ExecuteUpdateAsync(x => x.SetProperty(r => r.PeakPowerAchievedAtUtc, new DateTime(2020, 1, 1)));
        await db.Set<HrkPlayerRankingStats>().Where(x => x.PlayerId == second.Id || x.PlayerId == third.Id).ExecuteUpdateAsync(x => x.SetProperty(r => r.PeakPowerAchievedAtUtc, new DateTime(2020, 1, 2)));
        var reader = new LeaderboardReader(uow, database.Templates(), null!, null!);
        var page1 = await reader.GetAsync(third.UserId, "power", 1, 2, default);
        var page2 = await reader.GetAsync(third.UserId, "power", 2, 2, default);
        Assert.Equal(new[] { first.Id, second.Id }, page1.Items.Select(x => x.Player.PlayerId));
        Assert.Equal(third.Id, page2.Items[0].Player.PlayerId);
        Assert.Equal(3, page2.Items[0].Rank);
        Assert.Equal(3, page1.MyEntry!.Rank);
        Assert.True(page1.MyEntry.IsMe);
        Assert.DoesNotContain("userId", System.Text.Json.JsonSerializer.Serialize(page1), StringComparison.OrdinalIgnoreCase);
        await Assert.ThrowsAsync<BusinessException>(() => reader.GetAsync(first.UserId, "wallet-balance", 1, 20, default));
        await Assert.ThrowsAsync<BusinessException>(() => reader.GetAsync(first.UserId, "power", int.MaxValue, 100, default));
        await Assert.ThrowsAsync<BusinessException>(() => reader.GetHeroAsync(first.Id, 987654321, default));
    }

    [SummonSqlFact]
    public async Task ConcurrentSpendersSerializeWalletReadsAndAccumulateEntityTotals()
    {
        var player = await database.PlayerAsync(diamonds: 100);
        async Task Spend()
        {
            await using var db = database.Context();
            await using var uow = new UnitOfWork<GameDbContext>(db);
            var locks = RankingTestSupport.Locks(uow);
            var service = new DiamondSpendingService(uow, locks,
                new PlayerRankingStatsStore(uow, locks, TimeProvider.System), TimeProvider.System);
            await uow.ExecuteInTransactionAsync(async () =>
            {
                await locks.LockWalletAsync(player.Id);
                var wallet = await uow.Repository<HrkPlayerWallet>().Query().SingleAsync(x => x.PlayerId == player.Id);
                await service.SpendAsync(wallet, 20, "TEST", "concurrent", Guid.NewGuid());
            });
        }
        await Task.WhenAll(Spend(), Spend(), Spend());
        await using var read = database.Context();
        Assert.Equal(40, (await read.PlayerWallets.SingleAsync(x => x.PlayerId == player.Id)).Diamonds);
        Assert.Equal(60, (await read.Set<HrkPlayerRankingStats>().SingleAsync(x => x.PlayerId == player.Id)).TotalDiamondsSpent);
        Assert.Equal(3, await read.Set<HrkPlayerDiamondSpendTransaction>().CountAsync(x => x.PlayerId == player.Id));
    }

    [SummonSqlFact]
    public async Task RepeatedSpendingWithinOneTransactionUsesOneStatsEntityAndReplaysItsReceipt()
    {
        var player = await database.PlayerAsync(diamonds: 100);
        await using var db = database.Context();
        await using var uow = new UnitOfWork<GameDbContext>(db);
        var locks = RankingTestSupport.Locks(uow);
        var service = new DiamondSpendingService(uow, locks,
            new PlayerRankingStatsStore(uow, locks, TimeProvider.System), TimeProvider.System);
        await uow.ExecuteInTransactionAsync(async () =>
        {
            await locks.LockWalletAsync(player.Id);
            var wallet = await uow.Repository<HrkPlayerWallet>().Query().SingleAsync(x => x.PlayerId == player.Id);
            var requestId = Guid.NewGuid();
            Assert.True(await service.SpendAsync(wallet, 20, "TEST", "first", requestId));
            Assert.False(await service.SpendAsync(wallet, 20, "TEST", "first", requestId));
            Assert.True(await service.SpendAsync(wallet, 15, "TEST", "second", Guid.NewGuid()));
        });
        Assert.Equal(65, (await db.PlayerWallets.AsNoTracking().SingleAsync(x => x.PlayerId == player.Id)).Diamonds);
        Assert.Equal(35, (await db.Set<HrkPlayerRankingStats>().AsNoTracking().SingleAsync(x => x.PlayerId == player.Id)).TotalDiamondsSpent);
        Assert.Single(db.ChangeTracker.Entries<HrkPlayerRankingStats>());
    }

    [SummonSqlFact]
    public async Task StaleTrackedRankingCannotOverwriteANewerPeak()
    {
        var player = await database.PlayerAsync();
        await using (var initialDb = database.Context())
        {
            await using var initial = new UnitOfWork<GameDbContext>(initialDb);
            await initial.ExecuteInTransactionAsync(() => RankingTestSupport.Writer(initial, 100).RefreshPowerAsync(player.Id));
        }
        await using var staleDb = database.Context();
        await using var stale = new UnitOfWork<GameDbContext>(staleDb);
        await stale.Repository<HrkPlayerRankingStats>().Query().SingleAsync(x => x.PlayerId == player.Id);
        await using (var updatedDb = database.Context())
        {
            await using var updated = new UnitOfWork<GameDbContext>(updatedDb);
            await updated.ExecuteInTransactionAsync(() => RankingTestSupport.Writer(updated, 900).RefreshPowerAsync(player.Id));
        }
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => stale.ExecuteInTransactionAsync(
            () => RankingTestSupport.Writer(stale, 200).RefreshPowerAsync(player.Id)));
        Assert.Equal(900, (await staleDb.Set<HrkPlayerRankingStats>().AsNoTracking().SingleAsync(x => x.PlayerId == player.Id)).PeakFormationPower);
    }
}
