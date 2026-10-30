using System.Text.Json;
using Core.Common.Errors;
using Core.Persistence.Repositories;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;
using GAME.Domain.Entities;
using GAME.Infrastructure.Data;
using GAME.Application.Services;
using GAME.Application.Templates;
using GAME.Infrastructure.Templates;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace GAME.Domain.Tests;

public partial class SummonTransactionTests(SummonDatabaseFixture database) : IClassFixture<SummonDatabaseFixture>
{
    [SummonSqlFact]
    public async Task StaminaQueryRecoversDetachedSnapshotWithoutChangingPersistedOrTrackedPlayer()
    {
        var player = await database.PlayerAsync();
        await using var db = database.Context();
        var tracked = await db.Players.SingleAsync(x => x.Id == player.Id);
        tracked.Stamina = 1;
        tracked.MaxStamina = 100;
        tracked.LastStaminaRegeneratedOn = DateTime.UtcNow.AddMinutes(-13);
        tracked.StaminaPurchaseDate = DateTime.UtcNow.AddDays(-1);
        tracked.DailyStaminaPurchaseCount = 4;
        await db.SaveChangesAsync();
        var unitOfWork = new UnitOfWork<GameDbContext>(db);
        var handler = new GAME.Application.Features.Queries.Dungeon.GetStaminaQueryHandler(unitOfWork, TestGamePlayers.Create(unitOfWork));
        var response = await handler.Handle(new(player.UserId), default);
        Assert.Equal(3, response.Data!.Current);
        Assert.Equal(0, response.Data.PurchaseCount);
        Assert.Equal(1, tracked.Stamina);
        Assert.Equal(4, tracked.DailyStaminaPurchaseCount);
        await unitOfWork.SaveChangesAsync();
        await using var verify = database.Context();
        var persisted = await verify.Players.SingleAsync(x => x.Id == player.Id);
        Assert.Equal(1, persisted.Stamina);
        Assert.Equal(4, persisted.DailyStaminaPurchaseCount);
    }

    [SummonSqlFact]
    public async Task SharedPlayerLookupsKeepReadsDetachedAndPersistTrackedPlayerAndWalletChanges()
    {
        var player = await database.PlayerAsync(gold: 100);
        await using var db = database.Context();
        var unitOfWork = new UnitOfWork<GameDbContext>(db);
        var players = TestGamePlayers.Create(unitOfWork);
        var readOnly = (await players.GetPlayerByUserIdAsync(player.UserId))!;
        Assert.False(unitOfWork.IsTracked(readOnly));
        var originalExp = readOnly.Exp;
        readOnly.Exp += 500;
        var tracked = (await players.GetPlayerByIdAsync(player.Id, tracked: true))!;
        var wallet = (await players.GetWalletByPlayerIdAsync(player.Id))!;
        Assert.True(unitOfWork.IsTracked(tracked));
        Assert.True(unitOfWork.IsTracked(wallet));
        tracked.Exp += 10;
        wallet.Gold -= 5;
        await unitOfWork.SaveChangesAsync();
        await using var verify = database.Context();
        Assert.Equal(originalExp + 10, (await verify.Players.SingleAsync(x => x.Id == player.Id)).Exp);
        Assert.Equal(95, (await verify.PlayerWallets.SingleAsync(x => x.PlayerId == player.Id)).Gold);
    }

    [SummonSqlFact]
    public async Task PlayerIdLookupPreservesBackgroundLookupOfInactivePlayers()
    {
        var player = await database.PlayerAsync();
        await using var db = database.Context();
        await db.Players.Where(x => x.Id == player.Id).ExecuteUpdateAsync(set => set.SetProperty(x => x.IsActive, false));
        var unitOfWork = new UnitOfWork<GameDbContext>(db);
        var players = TestGamePlayers.Create(unitOfWork);
        Assert.Null(await players.GetPlayerByUserIdAsync(player.UserId));
        Assert.Null(await players.GetPlayerByIdAsync(-1));
        var readOnly = (await players.GetPlayerByIdAsync(player.Id))!;
        Assert.Equal(player.Id, readOnly.Id);
        Assert.False(readOnly.IsActive);
        Assert.False(unitOfWork.IsTracked(readOnly));
    }

    [SummonSqlFact]
    public async Task ConfigurationReturnsActualEnabledWeightsAndCurrentWallet()
    {
        var player = await database.PlayerAsync(gold: 1234, diamonds: 56);
        await using var db = database.Context();
        var configuration = await database.Service(db).GetConfigurationAsync(player.UserId, default);
        Assert.Equal(1234, configuration.Wallet.Gold);
        Assert.Equal(56, configuration.Wallet.Diamonds);
        Assert.Equal(2, configuration.Pools.Count);
        Assert.All(configuration.Pools, pool => Assert.Equal(100m, pool.Heroes.Sum(x => x.ProbabilityPercent)));
    }

    [SummonSqlFact]
    public async Task DisabledPoolRejectsNewDrawsButKeepsCommittedReceiptsReplayable()
    {
        var player = await database.PlayerAsync();
        await using var db = database.Context();
        var request = new SummonRequestDto(Guid.NewGuid(), "NORMAL", 1);
        var original = await database.Service(db).SummonAsync(player.UserId, request, default);
        await db.SummonPools.Where(x => x.Code == "NORMAL").ExecuteUpdateAsync(set => set.SetProperty(x => x.IsEnabled, false));
        try
        {
            var service = database.Service(db, new NoRollRandom());
            var replay = await service.SummonAsync(player.UserId, request, default);
            Assert.Equal(JsonSerializer.Serialize(original), JsonSerializer.Serialize(replay));
            var error = await Assert.ThrowsAsync<BusinessException>(() => service.SummonAsync(player.UserId, request with { RequestId = Guid.NewGuid() }, default));
            Assert.Equal("SUMMON_POOL_UNAVAILABLE", error.Code);
            Assert.Equal(990, (await db.PlayerWallets.SingleAsync(x => x.PlayerId == player.Id)).Gold);
        }
        finally
        {
            await db.SummonPools.Where(x => x.Code == "NORMAL").ExecuteUpdateAsync(set => set.SetProperty(x => x.IsEnabled, true));
        }
    }

    [SummonSqlFact]
    public async Task SingleDrawCreatesInitializedHeroAndImmutableReceipt()
    {
        var player = await database.PlayerAsync();
        await using var db = database.Context();
        var result = await database.Service(db).SummonAsync(player.UserId, new(Guid.NewGuid(), "NORMAL", 1), default);
        Assert.True(Assert.Single(result.Items).IsNew);
        Assert.Equal(990, result.Wallet.Gold);
        db.ChangeTracker.Clear();
        var hero = await db.PlayerHeroes.SingleAsync(x => x.PlayerId == player.Id);
        Assert.Equal(1, hero.Level);
        Assert.Equal(1, hero.Stars);
        Assert.Equal(123, hero.MaxExp);
        Assert.Equal(999, hero.Power);
        Assert.Single(await db.PlayerHeroBonusAttributes.Where(x => x.PlayerHeroId == hero.Id)
            .ToListAsync());
        Assert.Equal(JsonSerializer.Serialize(result), JsonSerializer.Serialize(await database.Service(db).GetResultAsync(player.UserId, result.RequestId, default)));
    }

    [SummonSqlFact]
    public async Task TenDrawsConvertDuplicatesInsideTheBatchAndRespectStackLimits()
    {
        var player = await database.PlayerAsync();
        await using var db = database.Context();
        var result = await database.Service(db).SummonAsync(player.UserId, new(Guid.NewGuid(), "NORMAL", 10), default);
        Assert.Equal(10, result.Items.Count);
        Assert.Single(result.Items, x => x.IsNew);
        Assert.Equal(9, result.Items.Count(x => !x.IsNew));
        Assert.Equal(180, result.Items.Sum(x => x.StoneQuantity));
        Assert.Equal(900, result.Wallet.Gold);
        Assert.Equal(180, await db.PlayerInventories.Where(x => x.PlayerId == player.Id).SumAsync(x => x.Count));
        Assert.All(await db.PlayerInventories.Where(x => x.PlayerId == player.Id)
            .ToListAsync(), x => Assert.InRange(x.Count, 1, 50));
        Assert.Equal(10, await db.SummonResultItems.CountAsync(x => x.Batch.PlayerId == player.Id));
        Assert.Equal(10, await db.HeroAcquisitionHistories.CountAsync(x => x.PlayerId == player.Id));
    }

    [SummonSqlFact]
    public async Task InsufficientGoldAndDiamondsLeaveNoRewardsOrHistory()
    {
        var player = await database.PlayerAsync(gold: 0, diamonds: 0);
        foreach (var code in new[]
        {
            "NORMAL",
            "PREMIUM"
        }

        )
        {
            await using var db = database.Context();
            var error = await Assert.ThrowsAsync<BusinessException>(() => database.Service(db).SummonAsync(player.UserId, new(Guid.NewGuid(), code, 1), default));
            Assert.Equal(code == "NORMAL" ? "INSUFFICIENT_GOLD" : "INSUFFICIENT_DIAMONDS", error.Code);
            Assert.False(await db.SummonBatches.AnyAsync(x => x.PlayerId == player.Id));
            Assert.False(await db.PlayerHeroes.AnyAsync(x => x.PlayerId == player.Id));
        }
    }

    [SummonSqlFact]
    public async Task FullInventoryIsRejectedBeforeAnyRewardRollAndWithoutCharging()
    {
        var player = await database.PlayerAsync(capacity: 0);
        await using var db = database.Context();
        var error = await Assert.ThrowsAsync<BusinessException>(() => database.Service(db, new NoRollRandom()).SummonAsync(player.UserId, new(Guid.NewGuid(), "NORMAL", 10), default));
        Assert.Equal("INVENTORY_FULL", error.Code);
        Assert.Equal(1000, (await db.PlayerWallets.SingleAsync(x => x.PlayerId == player.Id)).Gold);
        Assert.False(await db.PlayerHeroes.AnyAsync(x => x.PlayerId == player.Id));
        Assert.False(await db.HeroAcquisitionHistories.AnyAsync(x => x.PlayerId == player.Id));
        Assert.False(await db.SummonBatches.AnyAsync(x => x.PlayerId == player.Id));
    }

    [SummonSqlFact]
    public async Task DatabaseFailureWhileSavingRewardsRollsBackTheWholeBatch()
    {
        var player = await database.PlayerAsync();
        await using var db = database.Context();
        var trigger = "SummonFailure_" + Guid.NewGuid().ToString("N");
        // DDL identifiers cannot be parameters. Only our generated GUID and a database
        // generated Int64 are interpolated here; no caller input enters this test SQL.
#pragma warning disable EF1002
        await db.Database.ExecuteSqlRawAsync($"CREATE TRIGGER [{trigger}] ON dbo.HRK_SummonResultItems AFTER INSERT AS BEGIN IF EXISTS (SELECT 1 FROM inserted i JOIN dbo.HRK_SummonBatches b ON b.Id=i.BatchId WHERE b.PlayerId={player.Id}) THROW 51000, 'Intentional test failure', 1; END;");
        try
        {
            await Assert.ThrowsAsync<DbUpdateException>(() => database.Service(db).SummonAsync(player.UserId, new(Guid.NewGuid(), "NORMAL", 10), default));
            Assert.Equal(1000, (await db.PlayerWallets.SingleAsync(x => x.PlayerId == player.Id)).Gold);
            Assert.False(await db.PlayerHeroes.AnyAsync(x => x.PlayerId == player.Id));
            Assert.False(await db.HeroAcquisitionHistories.AnyAsync(x => x.PlayerId == player.Id));
            Assert.False(await db.SummonBatches.AnyAsync(x => x.PlayerId == player.Id));
            Assert.False(await db.PlayerInventories.AnyAsync(x => x.PlayerId == player.Id));
        }
        finally
        {
            await db.Database.ExecuteSqlRawAsync($"DROP TRIGGER [{trigger}];");
        }
#pragma warning restore EF1002
    }

    [SummonSqlFact]
    public async Task RepeatedRequestReturnsSameReceiptWithoutRerollOrExtraDebit()
    {
        var player = await database.PlayerAsync();
        var request = new SummonRequestDto(Guid.NewGuid(), "NORMAL", 1);
        await using var firstDb = database.Context();
        var first = await database.Service(firstDb).SummonAsync(player.UserId, request, default);
        await using var retryDb = database.Context();
        var second = await database.Service(retryDb, new NoRollRandom()).SummonAsync(player.UserId, request, default);
        Assert.Equal(JsonSerializer.Serialize(first), JsonSerializer.Serialize(second));
        Assert.Equal(990, (await retryDb.PlayerWallets.SingleAsync(x => x.PlayerId == player.Id)).Gold);
        Assert.Single(await retryDb.SummonBatches.Where(x => x.PlayerId == player.Id)
            .ToListAsync());
    }

    [SummonSqlFact]
    public async Task ReusingRequestIdWithDifferentPayloadIsRejected()
    {
        var player = await database.PlayerAsync();
        var request = new SummonRequestDto(Guid.NewGuid(), "NORMAL", 1);
        await using var db = database.Context();
        await database.Service(db).SummonAsync(player.UserId, request, default);
        db.ChangeTracker.Clear();
        var error = await Assert.ThrowsAsync<BusinessException>(() => database.Service(db).SummonAsync(player.UserId, request with { Count = 10 }, default));
        Assert.Equal("SUMMON_REQUEST_CONFLICT", error.Code);
        Assert.Equal(990, (await db.PlayerWallets.SingleAsync(x => x.PlayerId == player.Id)).Gold);
    }

    [SummonSqlFact]
    public async Task ConcurrentRequestsWithSameIdAreAppliedOnce()
    {
        var player = await database.PlayerAsync();
        var request = new SummonRequestDto(Guid.NewGuid(), "NORMAL", 10);
        await using var db1 = database.Context();
        await using var db2 = database.Context();
        var results = await Task.WhenAll(database.Service(db1).SummonAsync(player.UserId, request, default), database.Service(db2).SummonAsync(player.UserId, request, default));
        Assert.Equal(JsonSerializer.Serialize(results[0]), JsonSerializer.Serialize(results[1]));
        await using var check = database.Context();
        Assert.Equal(900, (await check.PlayerWallets.SingleAsync(x => x.PlayerId == player.Id)).Gold);
        Assert.Equal(1, await check.PlayerHeroes.CountAsync(x => x.PlayerId == player.Id));
    }

    [SummonSqlFact]
    public async Task ConcurrentDifferentRequestsCannotOverspendOrCreateDuplicateHeroes()
    {
        var player = await database.PlayerAsync(gold: 10);
        async Task<bool> Open()
        {
            await using var db = database.Context();
            try
            {
                await database.Service(db).SummonAsync(player.UserId, new(Guid.NewGuid(), "NORMAL", 1), default);
                return true;
            }
            catch (BusinessException error) when (error.Code == "INSUFFICIENT_GOLD")
            {
                return false;
            }
        }

        Assert.Equal(1, (await Task.WhenAll(Open(), Open())).Count(x => x));
        await using var check = database.Context();
        Assert.Equal(0, (await check.PlayerWallets.SingleAsync(x => x.PlayerId == player.Id)).Gold);
        Assert.Equal(1, await check.PlayerHeroes.CountAsync(x => x.PlayerId == player.Id));
    }

    [SummonSqlFact]
    public async Task AStaleWalletWriteFromAnotherFeatureCannotOverwriteTheDebit()
    {
        var player = await database.PlayerAsync();
        await using var stale = database.Context();
        var wallet = await stale.PlayerWallets.SingleAsync(x => x.PlayerId == player.Id);
        await using var summonDb = database.Context();
        await database.Service(summonDb).SummonAsync(player.UserId, new(Guid.NewGuid(), "NORMAL", 1), default);
        wallet.Gold -= 1;
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => stale.SaveChangesAsync());
        await using var check = database.Context();
        Assert.Equal(990, (await check.PlayerWallets.SingleAsync(x => x.PlayerId == player.Id)).Gold);
    }

    [SummonSqlFact]
    public async Task ReceiptsArePrivateToTheOwningPlayer()
    {
        var owner = await database.PlayerAsync();
        var stranger = await database.PlayerAsync();
        await using var db = database.Context();
        var result = await database.Service(db).SummonAsync(owner.UserId, new(Guid.NewGuid(), "NORMAL", 1), default);
        var error = await Assert.ThrowsAsync<BusinessException>(() => database.Service(db).GetResultAsync(stranger.UserId, result.RequestId, default));
        Assert.Equal("SUMMON_RESULT_NOT_FOUND", error.Code);
    }
}
