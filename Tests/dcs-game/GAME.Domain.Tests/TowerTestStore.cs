using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query;
using Core.Common.Repositories;

namespace GAME.Domain.Tests;

// In-memory repository double for service tests, not a substitute for SQL concurrency tests.
internal sealed class TowerTestStore : IUnitOfWork
{
    private readonly Dictionary<Type, object> _repositories = new();
    public List<T> Data<T>() where T : class => ((TestRepository<T>)Repository<T>()).Items;
    public IRepository<T> Repository<T>() where T : class
    {
        if (!_repositories.TryGetValue(typeof(T), out var repository))
            _repositories[typeof(T)] = repository = new TestRepository<T>();
        return (IRepository<T>)repository;
    }
    public IReadOnlyRepository<T> ReadOnlyRepository<T>() where T : class => Repository<T>();
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(1);
    public Task BeginTransactionAsync() => Task.CompletedTask;
    public Task CommitTransactionAsync() => Task.CompletedTask;
    public Task RollbackTransactionAsync() => Task.CompletedTask;
    public Task ExecuteStrategyAsync(Func<Task> operation) => operation();
    public Task ExecuteInTransactionAsync(Func<Task> action) => action();
    public void Dispose() { }
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}

internal sealed class TestRepository<T> : IRepository<T> where T : class
{
    public List<T> Items { get; } = new();
    public IQueryable<T> Query() => new TestAsyncQuery<T>(Items);
    public IQueryable<T> Table() => Query();
    public Task AddAsync(T entity) { Items.Add(entity); return Task.CompletedTask; }
    public void Update(T entity) { }
    public void Remove(T entity) => Items.Remove(entity);
    public Task<T?> GetByIdAsync<TKey>(TKey id, Expression<Func<T, TKey>> keySelector) =>
        Task.FromResult(Items.FirstOrDefault(x => EqualityComparer<TKey>.Default.Equals(keySelector.Compile()(x), id)));
    public Task<IEnumerable<T>> GetAllAsync() => Task.FromResult<IEnumerable<T>>(Items);
    public Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate) => Task.FromResult(Items.Any(predicate.Compile()));
    public Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.FirstOrDefault(predicate.Compile()));
}
internal sealed class TestAsyncQuery<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
{
    public TestAsyncQuery(IEnumerable<T> enumerable) : base(enumerable) { }
    public TestAsyncQuery(Expression expression) : base(expression) { }
    IQueryProvider IQueryable.Provider => new TestAsyncProvider(this);
    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default) =>
        new TestAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());
}
internal sealed class TestAsyncEnumerator<T>(IEnumerator<T> inner) : IAsyncEnumerator<T>
{
    public T Current => inner.Current;
    public ValueTask DisposeAsync() { inner.Dispose(); return ValueTask.CompletedTask; }
    public ValueTask<bool> MoveNextAsync() => ValueTask.FromResult(inner.MoveNext());
}
internal sealed class TestAsyncProvider(IQueryProvider inner) : IAsyncQueryProvider
{
    public IQueryable CreateQuery(Expression expression) => throw new NotSupportedException();
    public IQueryable<TElement> CreateQuery<TElement>(Expression expression) => new TestAsyncQuery<TElement>(expression);
    public object? Execute(Expression expression) => inner.Execute(expression);
    public TResult Execute<TResult>(Expression expression) => inner.Execute<TResult>(expression);
    public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken = default)
    {
        var resultType = typeof(TResult).GetGenericArguments()[0];
        var result = inner.Execute(expression);
        return (TResult)typeof(Task).GetMethod(nameof(Task.FromResult))!.MakeGenericMethod(resultType)
            .Invoke(null, new[] { result })!;
    }
}
