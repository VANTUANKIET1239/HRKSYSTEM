
namespace Core.Common.Repositories
{
    public interface  IReadOnlyRepository<T, TDbContext>
    {
        Task<T?> GetByIdAsync<TKey>(TKey id, Expression<Func<T, TKey>> keySelector);
        Task<IEnumerable<T>> GetAllAsync();

        IQueryable<T> Query();

        Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate);
    }
}
