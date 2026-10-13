using Microsoft.EntityFrameworkCore;

namespace Core.Persistence.Repositories
{
    public interface IUnitOfWork<TDbContext> : IUnitOfWork, IDisposable where TDbContext : DbContext
    {
        new IRepository<T, TDbContext> Repository<T>() where T : class;
        new IReadOnlyRepository<T, TDbContext> ReadOnlyRepository<T>() where T : class;
    }
}
