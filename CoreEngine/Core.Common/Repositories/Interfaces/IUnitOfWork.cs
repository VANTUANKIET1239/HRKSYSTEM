using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Core.Common.Repositories
{
    public interface IUnitOfWork : IDisposable
    {
        IRepository<T> Repository<T>() where T : class;
        IReadOnlyRepository<T> ReadOnlyRepository<T>() where T : class;
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
        ValueTask DisposeAsync();
        Task ExecuteStrategyAsync(Func<Task> operation);
        Task ExecuteInTransactionAsync(Func<Task> action);
    }

    public interface IUnitOfWork<TDbContext> : IUnitOfWork, IDisposable where TDbContext : DbContext
    {
        new IRepository<T, TDbContext> Repository<T>() where T : class;
        new IReadOnlyRepository<T, TDbContext> ReadOnlyRepository<T>() where T : class;
    }
}
