using System.Linq.Expressions;

namespace Core.Persistence.Repositories
{
    public interface IReadOnlyRepository<T> where T : class
    {
        Task<T?> GetByIdAsync<TKey>(TKey id, Expression<Func<T, TKey>> keySelector);
        Task<IEnumerable<T>> GetAllAsync();
        IQueryable<T> Query();
        Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate);
        Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
    }
}
