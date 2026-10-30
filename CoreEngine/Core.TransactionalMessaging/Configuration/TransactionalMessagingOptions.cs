namespace Core.TransactionalMessaging.Configuration;

public sealed class TransactionalMessagingOptions
{
    public const string SectionName = "TransactionalMessaging";

    public OutboxOptions Outbox { get; set; } = new();
    public InboxOptions Inbox { get; set; } = new();
}
