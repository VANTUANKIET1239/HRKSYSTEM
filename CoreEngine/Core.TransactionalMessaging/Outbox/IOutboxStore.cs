using Core.TransactionalMessaging.Configuration;
using Core.TransactionalMessaging.Entities;

namespace Core.TransactionalMessaging.Outbox;

public interface IOutboxStore
{
    Task<IReadOnlyList<OutboxMessage>> ClaimAsync(
        int batchSize,
        string lockToken,
        int leaseSeconds,
        CancellationToken ct);

    Task<int> MarkPublishedAsync(Guid id, string lockToken, CancellationToken ct);

    Task<int> MarkFailedAttemptAsync(
        OutboxMessage message,
        string lockToken,
        OutboxOptions options,
        Exception exception,
        bool permanent,
        CancellationToken ct);
}
