using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query;
using Core.Persistence.Repositories;
using GAME.Infrastructure.Services;
using Microsoft.AspNetCore.Http;

namespace GAME.Domain.Tests;
// In-memory repository double for service tests, not a substitute for SQL concurrency tests.
internal sealed class TowerTestStore : IUnitOfWork
{
    private readonly Dictionary<Type, object> _repositories = new();
    public Guid? CurrentTransactionId => null;
    public bool HasActiveTransaction { get; set; }
    public Core.Persistence.SqlExecutor.ISqlExecutor Sql => throw new NotSupportedException("SQL execution requires a relational test database.");

    public Task<int> ExecuteSqlAsync(FormattableString sql, CancellationToken cancellationToken = default) => throw new NotSupportedException("SQL locking requires a relational test database.");
    public Task<bool> CanConnectAsync(CancellationToken cancellationToken = default) => Task.FromResult(true);
    public bool IsTracked(object entity) => throw new NotSupportedException("Use EF tests to verify entity tracking.");
    public void Detach(object entity) => throw new NotSupportedException("Use EF tests to verify entity tracking.");
    public void ClearTracking() => throw new NotSupportedException("Use EF tests to verify entity tracking.");
    public List<T> Data<T>()
        where T : class => ((TestRepository<T>)Repository<T>()).Items;
    public IRepository<T> Repository<T>()
        where T : class
    {
        if (!_repositories.TryGetValue(typeof(T), out var repository))
            _repositories[typeof(T)] = repository = new TestRepository<T>();
        return (IRepository<T>)repository;
    }

    public int ReadOnlyQueries { get; private set; }
    public IReadOnlyRepository<T> ReadOnlyRepository<T>() where T : class
    {
        ReadOnlyQueries++;
        return Repository<T>();
    }
    public int SaveCount { get; private set; }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SaveCount++;
        return Task.FromResult(1);
    }

    public Task BeginTransactionAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task CommitTransactionAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task RollbackTransactionAsync() => Task.CompletedTask;
    public Task ExecuteStrategyAsync(Func<Task> operation, CancellationToken cancellationToken = default) => operation();
    public Task ExecuteInTransactionAsync(Func<Task> action, CancellationToken cancellationToken = default) => action();
    public void Dispose()
    {
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
