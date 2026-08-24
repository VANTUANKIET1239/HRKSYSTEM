
namespace Core.Common.Repositories
{
    public class Repository<T, TDbContext> : IRepository<T, TDbContext> 
        where T : class
        where TDbContext : DbContext 
    {
        protected readonly TDbContext _context;
        protected readonly DbSet<T> _dbSet;

        public Repository(TDbContext context)
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
            return await _dbSet.FirstOrDefaultAsync(lambda) ?? null;
        }

        public virtual async Task<IEnumerable<T>> GetAllAsync()
        {
            return await _dbSet.ToListAsync();
        }

        public virtual async Task AddAsync(T entity)
        {
            await _dbSet.AddAsync(entity);
        }

        public virtual void Update(T entity)
        {
            _dbSet.Update(entity);
        }

        public virtual void Remove(T entity)
        {
            _dbSet.Remove(entity);
        }

        public virtual IQueryable<T> Table()
        {
            return _dbSet.AsQueryable();
        }

        public virtual async Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.AnyAsync(predicate);
        }

        public IQueryable<T> Query()
        {
            return _dbSet.AsQueryable();
        }

        public virtual async Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
        {
            return await _dbSet.FirstOrDefaultAsync(predicate, cancellationToken);
        }
    }
}
