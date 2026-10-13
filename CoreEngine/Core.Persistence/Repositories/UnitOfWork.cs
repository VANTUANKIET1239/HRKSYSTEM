using System;
using System.Collections.Generic;

using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Core.Persistence.Repositories
{
    public class UnitOfWork<TDbContext> : IUnitOfWork<TDbContext>, IDisposable, IAsyncDisposable where TDbContext : DbContext
    {
        private readonly TDbContext _context;
        private readonly Dictionary<Type, object> _repositories = new();
        private readonly Dictionary<Type, object> _readOnlyRepositories = new();
        private IDbContextTransaction? _transaction;
        private long _completedCommits;
        private Core.Persistence.SqlExecutor.ISqlExecutor? _sql;

        public Core.Persistence.SqlExecutor.ISqlExecutor Sql =>
            _sql ??= new Core.Persistence.SqlExecutor.SqlExecutor(_context);

        public Guid? CurrentTransactionId => _context.Database.CurrentTransaction?.TransactionId;
        public bool HasActiveTransaction => CurrentTransactionId.HasValue;

        public Task<int> ExecuteSqlAsync(FormattableString sql, CancellationToken cancellationToken = default) =>
            _context.Database.ExecuteSqlInterpolatedAsync(sql, cancellationToken);

        public Task<bool> CanConnectAsync(CancellationToken cancellationToken = default) =>
            _context.Database.CanConnectAsync(cancellationToken);

        public bool IsTracked(object entity) => _context.Entry(entity).State != EntityState.Detached;
        public void Detach(object entity) => _context.Entry(entity).State = EntityState.Detached;
        public void ClearTracking() => _context.ChangeTracker.Clear();
        private readonly ILogger<UnitOfWork<TDbContext>>? _logger;
        private readonly int _slowTransactionMilliseconds;

        public UnitOfWork(TDbContext context, ILogger<UnitOfWork<TDbContext>>? logger = null,
            IConfiguration? configuration = null)
        {
            _context = context;
            _logger = logger;
            _slowTransactionMilliseconds = Math.Max(1, configuration?.GetValue<int?>("Database:SlowTransactionMilliseconds") ?? 1000);
        }

        public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            if (_context.Database.CurrentTransaction != null || _transaction != null)
            {
                throw new InvalidOperationException("A transaction is already active. Nested transaction ownership is not supported.");
            }

            _transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        }

        public int SaveChanges()
        {
            return _context.SaveChanges();
        }

        public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
        {
            var transaction = _transaction ?? throw new InvalidOperationException("There is no owned transaction to commit.");
            var started = Stopwatch.GetTimestamp();
            var commitFailed = false;
            var commitAttempted = false;
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                commitAttempted = true;
                await transaction.CommitAsync(cancellationToken);
                _completedCommits++;
                var elapsed = Stopwatch.GetElapsedTime(started).TotalMilliseconds;
                _logger?.Log(elapsed >= _slowTransactionMilliseconds ? LogLevel.Warning : LogLevel.Debug,
                    "Committed transaction in {DurationMs} ms for {DbContext}", elapsed, typeof(TDbContext).Name);
            }
            catch (Exception exception)
            {
                commitFailed = true;
                if (!commitAttempted)
                    throw;
                // A lost commit acknowledgement cannot safely be replayed blindly.
                throw new TransactionCommitOutcomeUnknownException(exception);
            }
            finally
            {
                _transaction = null;
                try
                {
                    await transaction.DisposeAsync();
                }
                catch (Exception cleanupException) when (commitFailed)
                {
                    _logger?.LogWarning(cleanupException, "Failed to dispose transaction after an unconfirmed commit");
                }
            }
        }

        public async Task RollbackTransactionAsync()
        {
            try
            {
                if (_transaction != null)
                {
                    await _transaction.RollbackAsync();
                }
            }
            finally
            {
                if (_transaction != null)
                {
                    var transaction = _transaction;
                    _transaction = null;
                    await transaction.DisposeAsync();
                }
            }
        }

        public async Task ExecuteStrategyAsync(Func<Task> operation, CancellationToken cancellationToken = default)
        {
            var strategy = _context.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async token =>
            {
                token.ThrowIfCancellationRequested();
                var completedCommits = _completedCommits;
                try
                {
                    await operation();
                }
                catch (Exception exception) when (_completedCommits != completedCommits)
                {
                    throw new InvalidOperationException("The operation failed after committing. It cannot be safely replayed.", exception);
                }
            }, cancellationToken);
        }

        public async Task ExecuteInTransactionAsync(Func<Task> action, CancellationToken cancellationToken = default)
        {
            if (_context.Database.CurrentTransaction != null || _transaction != null)
                throw new InvalidOperationException("ExecuteInTransactionAsync must own its transaction; nesting is not supported.");
            if (_context.ChangeTracker.HasChanges())
                throw new InvalidOperationException("Pending changes must be made inside the transaction action so they can be rebuilt on retry.");

            var attempt = 0;
            await ExecuteStrategyAsync(async () =>
            {
                if (attempt++ > 0)
                    _context.ChangeTracker.Clear();
                await BeginTransactionAsync(cancellationToken);
                try
                {
                    await action();
                    await SaveChangesAsync(cancellationToken);
                    await CommitTransactionAsync(cancellationToken);
                }
                catch
                {
                    try
                    {
                        await RollbackTransactionAsync(); // Cleanup must run even after cancellation.
                    }
                    finally
                    {
                        _context.ChangeTracker.Clear();
                    }
                    throw;
                }
            }, cancellationToken);
        }

        public IRepository<T, TDbContext> Repository<T>() where T : class
        {
            var type = typeof(T);
            if (!_repositories.ContainsKey(type))
            {
                _repositories[type] = new Repository<T, TDbContext>(_context);
            }

            return (IRepository<T, TDbContext>)_repositories[type];
        }

        IRepository<T> IUnitOfWork.Repository<T>() where T : class
        {
            return Repository<T>();
        }

        public IReadOnlyRepository<T, TDbContext> ReadOnlyRepository<T>() where T : class
        {
            var type = typeof(T);
            if (!_readOnlyRepositories.ContainsKey(type))
            {
                _readOnlyRepositories[type] = new ReadOnlyRepository<T, TDbContext>(_context);
            }

            return (IReadOnlyRepository<T, TDbContext>)_readOnlyRepositories[type];
        }

        IReadOnlyRepository<T> IUnitOfWork.ReadOnlyRepository<T>() where T : class
        {
            return ReadOnlyRepository<T>();
        }

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var started = Stopwatch.GetTimestamp();
            var result = await _context.SaveChangesAsync(cancellationToken);
            var elapsed = Stopwatch.GetElapsedTime(started).TotalMilliseconds;
            _logger?.Log(elapsed >= _slowTransactionMilliseconds ? LogLevel.Warning : LogLevel.Debug,
                "Saved {ChangedEntries} entries in {DurationMs} ms for {DbContext}", result, elapsed, typeof(TDbContext).Name);
            return result;
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        public async ValueTask DisposeAsync()
        {
            if (_transaction != null)
            {
                await _transaction.DisposeAsync();
            }

            await _context.DisposeAsync();
        }
    }
}
