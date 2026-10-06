namespace Core.TransactionalMessaging.Inbox;

public interface IInboxStore
{
    Task<bool> TryReserveAsync(
        string consumerName,
        string messageId,
        CancellationToken ct = default);

    Task<int> DeleteExpiredAsync(
        DateTime olderThanUtc,
        int batchSize,
        CancellationToken ct = default);
}
