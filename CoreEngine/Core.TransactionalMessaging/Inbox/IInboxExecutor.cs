namespace Core.TransactionalMessaging.Inbox;

public interface IInboxExecutor
{
    Task<InboxExecutionResult<T>> ExecuteAsync<T>(
        string consumerName,
        string messageId,
        Func<CancellationToken, Task<T>> action,
        CancellationToken ct = default);

    Task<InboxExecutionResult<T>> ExecuteInCurrentTransactionAsync<T>(
        string consumerName,
        string messageId,
        Func<CancellationToken, Task<T>> action,
        CancellationToken ct = default);
}
