namespace Core.TransactionalMessaging.Entities;

public sealed class InboxMessage
{
    public string ConsumerName { get; set; } = null!;
    public string MessageId { get; set; } = null!;
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
}
