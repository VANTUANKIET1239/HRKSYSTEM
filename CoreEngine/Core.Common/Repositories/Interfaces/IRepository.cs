

namespace Core.Common.Repositories
{
    public interface IRepository<T, TDbContext> : IReadOnlyRepository<T, TDbContext> where T : class
    {
        Task AddAsync(T entity);
        void Update(T entity);  
        void Remove(T entity);
    }
}
