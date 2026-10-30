using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query;
using Core.Persistence.Repositories;
using GAME.Infrastructure.Services;
using Microsoft.AspNetCore.Http;

namespace GAME.Domain.Tests;

internal sealed class TestRepository<T> : IRepository<T> where T : class
{
    public List<T> Items { get; } = new();

    public IQueryable<T> Query() => new TestAsyncQuery<T>(Items);
    public IQueryable<T> Table() => Query();
    public Task AddAsync(T entity)
    {
        Items.Add(entity);
        return Task.CompletedTask;
    }

    public void Update(T entity)
    {
    }

    public void Remove(T entity) => Items.Remove(entity);
    public Task<T?> GetByIdAsync<TKey>(TKey id, Expression<Func<T, TKey>> keySelector) => Task.FromResult(Items.FirstOrDefault(x => EqualityComparer<TKey>.Default.Equals(keySelector.Compile()(x), id)));
    public Task<IEnumerable<T>> GetAllAsync() => Task.FromResult<IEnumerable<T>>(Items);
    public Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate) => Task.FromResult(Items.Any(predicate.Compile()));
    public Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default) => Task.FromResult(Items.FirstOrDefault(predicate.Compile()));
}
