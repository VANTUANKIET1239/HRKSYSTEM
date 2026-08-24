
namespace Core.Common.Repositories
{
    public interface IRepository<T> : IReadOnlyRepository<T> where T : class
    {
        Task AddAsync(T entity);
        void Update(T entity);  
        void Remove(T entity);
        IQueryable<T> Table();
    }

    public interface IRepository<T, TDbContext> : IRepository<T>, IReadOnlyRepository<T, TDbContext> 
        where T : class
    {
    }
}
