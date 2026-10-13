using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using Core.Persistence.Repositories;
using GAME.Infrastructure.Data;
using GAME.Application.Services;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GAME.Domain.Tests;

public class UnitOfWorkTransactionTests
{
    [Fact]
    public async Task DetachingFailedInsertPreservesOtherPendingChanges()
    {
        using var connection = new ProbeConnection();
        await using var db = CreateContext(connection);
        await using var uow = new UnitOfWork<ProbeContext>(db);
        var failedInsert = new Probe { Id = 1 };
        var pending = new Probe { Id = 2 };
        await uow.Repository<Probe>().AddAsync(failedInsert);
        await uow.Repository<Probe>().AddAsync(pending);

        uow.Detach(failedInsert);

        Assert.False(uow.IsTracked(failedInsert));
        Assert.True(uow.IsTracked(pending));
        Assert.Same(pending, Assert.Single(db.ChangeTracker.Entries()).Entity);
        Assert.Equal(EntityState.Added, db.Entry(pending).State);
    }

    [Fact]
    public async Task BusinessFailure_RollsBackAndPropagatesWithoutRetryOrSave()
    {
        using var connection = new ProbeConnection();
        await using var db = CreateContext(connection, retries: true);
        await using var uow = new UnitOfWork<ProbeContext>(db);
        var attempts = 0;
        var original = new Core.Common.Errors.BusinessException("INSUFFICIENT_GOLD", "Không đủ Vàng.");
        var error = await Assert.ThrowsAsync<Core.Common.Errors.BusinessException>(() => uow.ExecuteInTransactionAsync(() =>
        {
            attempts++;
            db.Add(new Probe { Id = 1 });
            throw original;
        }));
        Assert.Same(original, error);
        Assert.Equal(1, attempts);
        Assert.Equal(1, connection.Rollbacks);
        Assert.Equal(0, connection.Commits);
        Assert.Equal(0, db.SaveCount);
        Assert.Empty(db.ChangeTracker.Entries());
        Assert.False(uow.HasActiveTransaction);
    }

    [Fact]
    public async Task CancelledCommit_DoesNotCommitAndReleasesTransaction()
    {
        using var connection = new ProbeConnection();
        await using var db = CreateContext(connection);
        await using var uow = new UnitOfWork<ProbeContext>(db);
        await uow.BeginTransactionAsync();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => uow.CommitTransactionAsync(cancellation.Token));
        Assert.Equal(0, connection.Commits);
        Assert.Null(db.Database.CurrentTransaction);
        Assert.False(uow.HasActiveTransaction);
    }

    [Fact]
    public async Task Wrapper_RejectsPendingChangesOutsideTheReplayableAction()
    {
        using var connection = new ProbeConnection();
        await using var db = CreateContext(connection);
        await using var uow = new UnitOfWork<ProbeContext>(db);
        db.Add(new Probe { Id = 1 });
        await Assert.ThrowsAsync<InvalidOperationException>(() => uow.ExecuteInTransactionAsync(() => Task.CompletedTask));
        Assert.True(db.ChangeTracker.HasChanges());
        Assert.Null(db.Database.CurrentTransaction);
    }

    [Fact]
    public async Task Commit_DoesNotSavePendingChanges()
    {
        using var connection = new ProbeConnection();
        await using var db = CreateContext(connection);
        await using var uow = new UnitOfWork<ProbeContext>(db);
        await uow.BeginTransactionAsync();
        db.Add(new Probe { Id = 1 });
        await uow.CommitTransactionAsync();
        Assert.Equal(0, db.SaveCount);
        Assert.True(db.ChangeTracker.HasChanges());
        Assert.Equal(1, connection.Commits);
        Assert.Null(db.Database.CurrentTransaction);
    }

    [Fact]
    public async Task Wrapper_SavesOnceAndCommits()
    {
        using var connection = new ProbeConnection();
        await using var db = CreateContext(connection);
        await using var uow = new UnitOfWork<ProbeContext>(db);
        await uow.ExecuteInTransactionAsync(() => { db.Add(new Probe { Id = 1 }); return Task.CompletedTask; });
        Assert.Equal(1, db.SaveCount);
        Assert.Equal(1, connection.Commits);
    }

    [Fact]
    public async Task NestedOwnership_IsRejectedWithoutFinishingOuterTransaction()
    {
        using var connection = new ProbeConnection();
        await using var db = CreateContext(connection);
        await using var uow = new UnitOfWork<ProbeContext>(db);
        await uow.BeginTransactionAsync();
        var outer = db.Database.CurrentTransaction;
        await Assert.ThrowsAsync<InvalidOperationException>(() => uow.BeginTransactionAsync());
        await Assert.ThrowsAsync<InvalidOperationException>(() => uow.ExecuteInTransactionAsync(() => Task.CompletedTask));
        Assert.Same(outer, db.Database.CurrentTransaction);
        Assert.Equal(0, connection.Commits + connection.Rollbacks);
        await uow.RollbackTransactionAsync();
    }

    [Fact]
    public async Task Retry_ClearsRolledBackStateAndRebuildsAction()
    {
        using var connection = new ProbeConnection();
        await using var db = CreateContext(connection, retries: true);
        await using var uow = new UnitOfWork<ProbeContext>(db);
        var attempts = 0;
        await uow.ExecuteInTransactionAsync(async () =>
        {
            Assert.Empty(db.ChangeTracker.Entries());
            db.Add(new Probe { Id = 1 });
            if (++attempts == 1)
            {
                await uow.SaveChangesAsync();
                throw new TimeoutException("Transient failure after an intermediate save.");
            }
        });
        Assert.Equal(2, attempts);
        Assert.Equal(1, connection.Rollbacks);
        Assert.Equal(1, connection.Commits);
        Assert.Single(db.ChangeTracker.Entries());
    }

    [Fact]
    public async Task FailedCommit_IsNotReplayed()
    {
        using var connection = new ProbeConnection { FailCommit = true };
        await using var db = CreateContext(connection, retries: true);
        await using var uow = new UnitOfWork<ProbeContext>(db);
        var attempts = 0;
        var error = await Assert.ThrowsAsync<TransactionCommitOutcomeUnknownException>(() =>
            uow.ExecuteInTransactionAsync(() => { attempts++; return Task.CompletedTask; }));
        Assert.IsType<TimeoutException>(error.InnerException);
        Assert.Equal(1, attempts);
        Assert.Equal(1, connection.Commits);
        Assert.Null(db.Database.CurrentTransaction);
    }

    [Fact]
    public async Task FailureAfterSuccessfulCommit_IsNotReplayed()
    {
        using var connection = new ProbeConnection();
        await using var db = CreateContext(connection, retries: true);
        await using var uow = new UnitOfWork<ProbeContext>(db);
        var attempts = 0;
        await Assert.ThrowsAsync<InvalidOperationException>(() => uow.ExecuteStrategyAsync(async () =>
        {
            attempts++;
            await uow.BeginTransactionAsync();
            await uow.CommitTransactionAsync();
            throw new TimeoutException("Failure building the response.");
        }));
        Assert.Equal(1, attempts);
        Assert.Equal(1, connection.Commits);
    }

    [Fact]
    public async Task Cancellation_RollsBackWithoutUsingCancelledTokenForCleanup()
    {
        using var connection = new ProbeConnection();
        await using var db = CreateContext(connection);
        await using var uow = new UnitOfWork<ProbeContext>(db);
        using var cancellation = new CancellationTokenSource();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => uow.ExecuteInTransactionAsync(() =>
        {
            cancellation.Cancel();
            return Task.CompletedTask;
        }, cancellation.Token));
        Assert.Equal(1, connection.Rollbacks);
        Assert.Equal(0, connection.Commits);
        Assert.Null(db.Database.CurrentTransaction);
    }

    [Fact]
    public async Task Tower_JoiningTransactionAcquiresEachPlayerLockWithoutCommitting()
    {
        using var connection = new ProbeConnection();
        var options = new DbContextOptionsBuilder<GameDbContext>().UseSqlServer(connection).Options;
        await using var db = new GameDbContext(options);
        await using var uow = new UnitOfWork<GameDbContext>(db);
        await uow.BeginTransactionAsync();
        var outer = db.Database.CurrentTransaction;
        var runner = new TowerOperationRunner(uow);
        await runner.RunAsync("one", () => Task.FromResult(1), default);
        await runner.RunAsync("two", () => Task.FromResult(2), default);
        Assert.Equal(new[] { "tower:player:one", "tower:player:two" }, connection.LockResources);
        Assert.Same(outer, db.Database.CurrentTransaction);
        Assert.Equal(0, connection.Commits + connection.Rollbacks);
        await uow.RollbackTransactionAsync();
    }

    private static ProbeContext CreateContext(ProbeConnection connection, bool retries = false) =>
        new(new DbContextOptionsBuilder<ProbeContext>()
            .UseSqlServer(connection, options =>
            {
                if (retries) options.EnableRetryOnFailure(1, TimeSpan.Zero, null);
            }).Options);

    private sealed class Probe { public int Id { get; set; } }
    private sealed class ProbeContext(DbContextOptions<ProbeContext> options) : DbContext(options)
    {
        public int SaveCount { get; private set; }
        protected override void OnModelCreating(ModelBuilder builder) => builder.Entity<Probe>().HasKey(x => x.Id);
        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            SaveCount++;
            ChangeTracker.AcceptAllChanges();
            return Task.FromResult(1);
        }
    }

    // Exercise EF's real relational transaction lifecycle without a database server.
    private sealed class ProbeConnection : DbConnection
    {
        private ConnectionState _state;
        public int Commits, Rollbacks;
        public bool FailCommit;
        public List<string> LockResources { get; } = new();
        [AllowNull]
        public override string ConnectionString { get; set; } = "Server=localhost;Database=Probe;Trusted_Connection=True";
        public override string Database => "Probe";
        public override string DataSource => "localhost";
        public override string ServerVersion => "16.0";
        public override ConnectionState State => _state;
        public override void Open() => _state = ConnectionState.Open;
        public override void Close() => _state = ConnectionState.Closed;
        public override void ChangeDatabase(string databaseName) { }
        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) => new ProbeTransaction(this, isolationLevel);
        protected override DbCommand CreateDbCommand() => new ProbeCommand(this);
    }

    private sealed class ProbeTransaction(ProbeConnection connection, IsolationLevel isolationLevel) : DbTransaction
    {
        public override IsolationLevel IsolationLevel => isolationLevel;
        protected override DbConnection DbConnection => connection;
        public override void Commit()
        {
            connection.Commits++;
            if (connection.FailCommit) throw new TimeoutException("Commit acknowledgement lost.");
        }
        public override void Rollback() => connection.Rollbacks++;
    }

    private sealed class ProbeCommand(ProbeConnection connection) : DbCommand
    {
        private readonly SqlCommand _parameters = new();
        [AllowNull]
        public override string CommandText { get; set; } = "";
        public override int CommandTimeout { get; set; }
        public override CommandType CommandType { get; set; }
        public override bool DesignTimeVisible { get; set; }
        public override UpdateRowSource UpdatedRowSource { get; set; }
        protected override DbConnection? DbConnection { get; set; } = connection;
        protected override DbTransaction? DbTransaction { get; set; }
        protected override DbParameterCollection DbParameterCollection => _parameters.Parameters;
        protected override DbParameter CreateDbParameter() => new SqlParameter();
        public override void Cancel() { }
        public override void Prepare() { }
        public override int ExecuteNonQuery()
        {
            Assert.Equal(CommandType.StoredProcedure, CommandType);
            Assert.Equal("[dbo].[usp_HRK_AcquireTowerPlayerLock]", CommandText);
            Assert.NotNull(DbTransaction);
            connection.LockResources.Add(Assert.IsType<string>(Parameters["Resource"].Value));
            return 0;
        }
        public override object? ExecuteScalar() => throw new NotSupportedException();
        protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior) => throw new NotSupportedException();
    }
}
