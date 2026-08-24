
namespace Core.Common.Repositories
{
    public class ReadOnlyRepository<T, TDbContext> : IReadOnlyRepository<T, TDbContext> 
        where T : class
         where TDbContext : DbContext
    {
        protected readonly TDbContext _context;
        protected readonly DbSet<T> _dbSet;

        public ReadOnlyRepository(TDbContext context)
        {
            _context = context;
            _dbSet = context.Set<T>();
        }

        public virtual async Task<T?> GetByIdAsync<TKey>(TKey id, Expression<Func<T, TKey>> keySelector)
        {
            var parameter = Expression.Parameter(typeof(T), "x");
            var property = Expression.Invoke(keySelector, parameter);
            var equals = Expression.Equal(property, Expression.Constant(id));

            var lambda = Expression.Lambda<Func<T, bool>>(equals, parameter);
            return await _dbSet.AsNoTracking().FirstOrDefaultAsync(lambda) ?? null;
        }

        public virtual async Task<IEnumerable<T>> GetAllAsync()
        {
            return await _dbSet.AsNoTracking().ToListAsync();
        }

        public virtual IQueryable<T> Query()
        {
            return _dbSet.AsNoTracking().AsQueryable();
        }

        public virtual async Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.AsNoTracking().AnyAsync(predicate);
        }

        public virtual async Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
        {
            return await _dbSet.AsNoTracking().FirstOrDefaultAsync(predicate, cancellationToken);
        }
    }
}
