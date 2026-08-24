using System.Linq.Expressions;

namespace Core.Common.Repositories
{
    public interface IReadOnlyRepository<T> where T : class
    {
        Task<T?> GetByIdAsync<TKey>(TKey id, Expression<Func<T, TKey>> keySelector);
        Task<IEnumerable<T>> GetAllAsync();
        IQueryable<T> Query();
        Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate);
        Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
    }

    public interface IReadOnlyRepository<T, TDbContext> : IReadOnlyRepository<T>
        where T : class
    {
    }
}
