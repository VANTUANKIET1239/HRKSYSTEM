using Microsoft.EntityFrameworkCore;

namespace Core.Persistence.Repositories
{
    public interface IReadOnlyRepository<T, TDbContext> : IReadOnlyRepository<T>
        where T : class
    {
    }
}
