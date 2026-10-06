using Microsoft.EntityFrameworkCore;

namespace Core.TransactionalMessaging.Inbox;

public sealed class InboxExecutor<TDbContext>(TDbContext db, IInboxStore store) : IInboxExecutor
    where TDbContext : DbContext
{
    public async Task<InboxExecutionResult<T>> ExecuteAsync<T>(
        string consumerName,
        string messageId,
        Func<CancellationToken, Task<T>> action,
        CancellationToken ct = default)
    {
        if (db.Database.CurrentTransaction is not null)
        {
            return await ExecuteInCurrentTransactionAsync(consumerName, messageId, action, ct);
        }

        InboxExecutionResult<T> result = default!;
        var strategy = db.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            db.ChangeTracker.Clear();
            await using var transaction = await db.Database.BeginTransactionAsync(ct);
            try
            {
                result = await ExecuteInCurrentTransactionAsync(consumerName, messageId, action, ct);
                await db.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);
            }
            catch
            {
                await transaction.RollbackAsync(CancellationToken.None);
                db.ChangeTracker.Clear();
                throw;
            }
        });

        return result;
    }

    public async Task<InboxExecutionResult<T>> ExecuteInCurrentTransactionAsync<T>(
        string consumerName,
        string messageId,
        Func<CancellationToken, Task<T>> action,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(consumerName);
        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);
        ArgumentNullException.ThrowIfNull(action);

        if (db.Database.CurrentTransaction is null)
        {
            throw new InvalidOperationException(
                "ExecuteInCurrentTransactionAsync requires an active database transaction.");
        }

        var reserved = await store.TryReserveAsync(consumerName, messageId, ct);
        if (!reserved)
        {
            return InboxExecutionResult<T>.Duplicate();
        }

        var result = await action(ct);
        return InboxExecutionResult<T>.Processed(result);
    }
}
