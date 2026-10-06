namespace Core.TransactionalMessaging.Inbox;

public sealed record InboxExecutionResult<T>(bool IsDuplicate, T Result)
{
    public static InboxExecutionResult<T> Processed(T result) => new InboxExecutionResult<T>(false, result);

    public static InboxExecutionResult<T> Duplicate() => new InboxExecutionResult<T>(true, default!);
}
