using System;
using System.Threading;
using System.Threading.Tasks;


namespace Core.Persistence.Repositories
{
    public interface IUnitOfWork : IDisposable
    {
        Guid? CurrentTransactionId { get; }
        bool HasActiveTransaction { get; }
        /// <summary>SQL access sharing this UnitOfWork's connection and current transaction.</summary>
        Core.Persistence.SqlExecutor.ISqlExecutor Sql { get; }
        /// <summary>Executes parameterized SQL on the same connection and transaction as the repositories.</summary>
        Task<int> ExecuteSqlAsync(FormattableString sql, CancellationToken cancellationToken = default);
        Task<bool> CanConnectAsync(CancellationToken cancellationToken = default);
        bool IsTracked(object entity);
        /// <summary>Stops tracking one entity without discarding other pending changes.</summary>
        void Detach(object entity);
        /// <summary>Discards tracked state. Call only when rebuilding an entire operation.</summary>
        void ClearTracking();
        IRepository<T> Repository<T>() where T : class;
        IReadOnlyRepository<T> ReadOnlyRepository<T>() where T : class;
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
        Task BeginTransactionAsync(CancellationToken cancellationToken = default);
        /// <summary>Commits the owned transaction. Does not save pending changes.</summary>
        Task CommitTransactionAsync(CancellationToken cancellationToken = default);
        Task RollbackTransactionAsync();
        ValueTask DisposeAsync();
        Task ExecuteStrategyAsync(Func<Task> operation, CancellationToken cancellationToken = default);
        /// <summary>
        /// Owns a transaction and performs a final save. Nested transactions are rejected.
        /// Pending changes must be created inside the action. The action must reload its
        /// entities on every attempt; retries discard tracked state.
        /// </summary>
        Task ExecuteInTransactionAsync(Func<Task> action, CancellationToken cancellationToken = default);
    }


}
