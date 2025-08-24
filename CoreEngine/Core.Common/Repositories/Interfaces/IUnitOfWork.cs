

namespace Core.Common.Repositories
{

    public interface IUnitOfWork<TDbContext> : IDisposable
    {
        IRepository<T, TDbContext> Repository<T>() where T : class;
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();

        ValueTask DisposeAsync();
    }

}
