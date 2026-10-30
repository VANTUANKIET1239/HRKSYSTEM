using System.Text.RegularExpressions;
using Core.Persistence.Repositories;
using GAME.Application.Common;
using GAME.Application.Services;
using GAME.Infrastructure.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GAME.Domain.Tests;

public class GameOperationProcedureTests(SummonDatabaseFixture database) : IClassFixture<SummonDatabaseFixture>
{
    [SummonSqlFact]
    public async Task LockProceduresRejectCallsWithoutACallerTransaction()
    {
        await using var db = database.Context();
        await using var uow = new UnitOfWork<GameDbContext>(db);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new TowerOperationRunner(uow, RankingTestSupport.Writer(uow), RankingTestSupport.Locks(uow)).LockAsync("tower:player:" + Guid.NewGuid(), default));
        var tower = await Assert.ThrowsAsync<SqlException>(() => uow.Sql.ExecuteProcedureAsync(
            GameStoredProcedures.AcquireTransactionLock, new { Resource = "test:no-transaction" }));
        var wallet = await Assert.ThrowsAsync<SqlException>(() =>
            uow.Sql.ExecuteProcedureAsync(GameStoredProcedures.LockPlayerWallet, new { PlayerId = -1L }));
        Assert.Equal(51001, tower.Number);
        Assert.Equal(51001, wallet.Number);
        Assert.False(uow.HasActiveTransaction);
    }

    [SummonSqlFact]
    public async Task TowerLockBlocksTheSameResourceAndIsReleasedByRollback()
    {
        await using var ownerDb = database.Context();
        await using var contenderDb = database.Context();
        await using var owner = new UnitOfWork<GameDbContext>(ownerDb);
        await using var contender = new UnitOfWork<GameDbContext>(contenderDb);
        var resource = "tower:player:O'Brien;--:" + Guid.NewGuid();
        await owner.BeginTransactionAsync();
        await new TowerOperationRunner(owner, RankingTestSupport.Writer(owner), RankingTestSupport.Locks(owner)).LockAsync(resource, default);
        await contender.BeginTransactionAsync();
        // A different player can continue while the first resource is locked.
        await new TowerOperationRunner(contender, RankingTestSupport.Writer(contender), RankingTestSupport.Locks(contender)).LockAsync(resource + ":other", default);
        var error = await Assert.ThrowsAsync<SqlException>(() => contender.Sql.ExecuteProcedureAsync(
            GameStoredProcedures.AcquireTowerPlayerLock, new { Resource = resource, LockTimeoutMilliseconds = 50 }));
        Assert.Equal(51000, error.Number);
        Assert.True(owner.HasActiveTransaction);

        await contender.RollbackTransactionAsync();
        await owner.RollbackTransactionAsync();
        await contender.BeginTransactionAsync();
        await new TowerOperationRunner(contender, RankingTestSupport.Writer(contender), RankingTestSupport.Locks(contender)).LockAsync(resource, default);
        await contender.CommitTransactionAsync();
    }

    [SummonSqlFact]
    public async Task WalletUpdateLockIsHeldUntilCallerCommitWithoutChangingTheWallet()
    {
        var player = await database.PlayerAsync(gold: 1234);
        await using var ownerDb = database.Context();
        await using var contenderDb = database.Context();
        await using var owner = new UnitOfWork<GameDbContext>(ownerDb);
        await using var contender = new UnitOfWork<GameDbContext>(contenderDb);
        var before = await ownerDb.PlayerWallets.AsNoTracking().SingleAsync(wallet => wallet.PlayerId == player.Id);
        await owner.BeginTransactionAsync();
        await RankingTestSupport.Locks(owner).LockWalletAsync(player.Id);

        await contender.BeginTransactionAsync();
        // Test-only session setting bounds the lock wait; production keeps its existing timeout policy.
        await contender.Sql.ExecuteAsync("SET LOCK_TIMEOUT 100");
        var error = await Assert.ThrowsAsync<SqlException>(() => contender.Sql.ExecuteProcedureAsync(
            GameStoredProcedures.LockPlayerWallet, new { PlayerId = player.Id }));
        Assert.Equal(1222, error.Number);
        Assert.True(owner.HasActiveTransaction);
        await contender.RollbackTransactionAsync();

        await owner.CommitTransactionAsync();
        await contender.BeginTransactionAsync();
        await contender.Sql.ExecuteProcedureAsync(GameStoredProcedures.LockPlayerWallet, new { PlayerId = player.Id });
        await contender.RollbackTransactionAsync();
        var after = await ownerDb.PlayerWallets.AsNoTracking().SingleAsync(wallet => wallet.PlayerId == player.Id);
        Assert.Equal(before.Gold, after.Gold);
        Assert.Equal(before.RowVersion, after.RowVersion);
    }

    [SummonSqlFact]
    public async Task TowerRunnerOwnsAndReleasesTheTransactionAroundARealProcedureCall()
    {
        await using var db = database.Context();
        await using var uow = new UnitOfWork<GameDbContext>(db);
        var runner = new TowerOperationRunner(uow, RankingTestSupport.Writer(uow), RankingTestSupport.Locks(uow));
        var userId = Guid.NewGuid().ToString("N");
        var result = await runner.RunAsync(userId, () =>
        {
            Assert.True(uow.HasActiveTransaction);
            return Task.FromResult(7);
        }, default);
        Assert.Equal(7, result);
        Assert.False(uow.HasActiveTransaction);

        await using var contenderDb = database.Context();
        await using var contender = new UnitOfWork<GameDbContext>(contenderDb);
        await contender.BeginTransactionAsync();
        await contender.Sql.ExecuteProcedureAsync(GameStoredProcedures.AcquireTowerPlayerLock,
            new { Resource = "tower:player:" + userId, LockTimeoutMilliseconds = 50 });
        await contender.RollbackTransactionAsync();
    }

    [SummonSqlFact]
    public async Task SharedLockCoordinatesWithLegacyProcedureAndRetriesAFailedAcquisition()
    {
        await using var ownerDb = database.Context();
        await using var contenderDb = database.Context();
        await using var owner = new UnitOfWork<GameDbContext>(ownerDb);
        await using var contender = new UnitOfWork<GameDbContext>(contenderDb);
        var locks = RankingTestSupport.Locks(contender);
        var resource = "tower:player:" + Guid.NewGuid();
        await owner.BeginTransactionAsync();
        await owner.Sql.ExecuteProcedureAsync(GameStoredProcedures.AcquireTowerPlayerLock, new { Resource = resource });
        await contender.BeginTransactionAsync();
        var error = await Assert.ThrowsAsync<SqlException>(() => locks.AcquireAsync(resource, timeoutMilliseconds: 50));
        Assert.Equal(51000, error.Number);
        await owner.RollbackTransactionAsync();
        await locks.AcquireAsync(resource, timeoutMilliseconds: 50);
        await contender.CommitTransactionAsync();

        // The same service must go to SQL again after the previous transaction ends.
        await owner.BeginTransactionAsync();
        await owner.Sql.ExecuteProcedureAsync(GameStoredProcedures.AcquireTowerPlayerLock, new { Resource = resource });
        await contender.BeginTransactionAsync();
        error = await Assert.ThrowsAsync<SqlException>(() => locks.AcquireAsync(resource, timeoutMilliseconds: 50));
        Assert.Equal(51000, error.Number);
        await contender.RollbackTransactionAsync();
        await owner.RollbackTransactionAsync();
    }
}
