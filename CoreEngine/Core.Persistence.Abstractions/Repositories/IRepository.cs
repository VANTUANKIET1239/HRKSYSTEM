
namespace Core.Persistence.Repositories
{
    public interface IRepository<T> : IReadOnlyRepository<T> where T : class
    {
        Task AddAsync(T entity);
        void Update(T entity);
        void Remove(T entity);
        IQueryable<T> Table();
    }
}
