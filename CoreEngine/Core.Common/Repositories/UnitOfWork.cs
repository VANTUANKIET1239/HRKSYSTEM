using System;
using System.Collections.Generic;
using System.Data.Entity.Validation;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Core.Common.Repositories
{
    public class UnitOfWork<TDbContext> : IUnitOfWork<TDbContext>, IDisposable, IAsyncDisposable where TDbContext : DbContext
    {
        private readonly TDbContext _context;
        private readonly Dictionary<Type, object> _repositories = new();
        private readonly Dictionary<Type, object> _readOnlyRepositories = new();
        private IDbContextTransaction? _transaction;
        private string _errorMessage = string.Empty;
        public bool HasActiveTransaction => _transaction != null;
        private readonly ILogger<UnitOfWork<TDbContext>>? _logger;
        private readonly int _slowTransactionMilliseconds;

        public UnitOfWork(TDbContext context, ILogger<UnitOfWork<TDbContext>>? logger = null,
            IConfiguration? configuration = null)
        {
            _context = context;
            _logger = logger;
            _slowTransactionMilliseconds = Math.Max(1, configuration?.GetValue<int?>("Database:SlowTransactionMilliseconds") ?? 1000);
        }

        public async Task BeginTransactionAsync()
        {
            if (_transaction != null)
            {
                return;
            }

            _transaction = await _context.Database.BeginTransactionAsync();
        }

        public int SaveChanges()
        {
            return _context.SaveChanges();
        }

        public async Task CommitTransactionAsync()
        {
            var started = Stopwatch.GetTimestamp();
            try
            {
                await _context.SaveChangesAsync();
                if (_transaction != null)
                {
                    await _transaction.CommitAsync();
                    var elapsed = Stopwatch.GetElapsedTime(started).TotalMilliseconds;
                    _logger?.Log(elapsed >= _slowTransactionMilliseconds ? LogLevel.Warning : LogLevel.Debug,
                        "Committed transaction in {DurationMs} ms for {DbContext}", elapsed, typeof(TDbContext).Name);
                }
            }
            catch
            {
                await RollbackTransactionAsync();
                throw;
            }
            finally
            {
                if (_transaction != null)
                {
                    await _transaction.DisposeAsync();
                    _transaction = null;
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
                    await _transaction.DisposeAsync();
                    _transaction = null;
                }
            }
        }

        public async Task ExecuteStrategyAsync(Func<Task> operation)
        {
            var strategy = _context.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(operation);
        }

        public async Task ExecuteInTransactionAsync(Func<Task> action)
        {
            var strategy = _context.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                await BeginTransactionAsync();
                try
                {
                    await action();
                    await SaveChangesAsync();
                    await CommitTransactionAsync();
                }
                catch
                {
                    await RollbackTransactionAsync();
                    throw;
                }
            });
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
            try
            {
                var result = await _context.SaveChangesAsync(cancellationToken);
                var elapsed = Stopwatch.GetElapsedTime(started).TotalMilliseconds;
                _logger?.Log(elapsed >= _slowTransactionMilliseconds ? LogLevel.Warning : LogLevel.Debug,
                    "Saved {ChangedEntries} entries in {DurationMs} ms for {DbContext}", result, elapsed, typeof(TDbContext).Name);
                return result;
            }
            catch (DbEntityValidationException dbEx)
            {
                _errorMessage = dbEx.InnerException?.Message ?? dbEx.Message;
                throw new Exception(_errorMessage, dbEx);
            }
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
