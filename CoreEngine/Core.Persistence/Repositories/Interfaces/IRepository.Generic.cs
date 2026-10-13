using Microsoft.EntityFrameworkCore;

namespace Core.Persistence.Repositories
{
    public interface IRepository<T, TDbContext> : IRepository<T>, IReadOnlyRepository<T, TDbContext> 
        where T : class
    {
    }
}
