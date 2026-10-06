namespace Core.TransactionalMessaging.Entities;

public enum OutboxStatus : byte
{
    Pending = 1,
    Processing = 2,
    Published = 3,
    Failed = 4
}

public sealed class OutboxMessage
{
    public Guid Id { get; set; }
    public string EventName { get; set; } = null!;
    public int EventVersion { get; set; } = 1;
    public string PublisherName { get; set; } = null!;
    public string RoutingKey { get; set; } = null!;
    public string Payload { get; set; } = null!;
    public OutboxStatus Status { get; set; } = OutboxStatus.Pending;
    public int RetryCount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? NextAttemptAt { get; set; }
    public DateTime? LastAttemptAt { get; set; }
    public DateTime? PublishedAt { get; set; }
    public string? LockToken { get; set; }
    public DateTime? LockedUntil { get; set; }
    public string? LastError { get; set; }
    public string? PartitionKey { get; set; }
    public long? Sequence { get; set; }
    public string? CorrelationId { get; set; }
    public string? TraceParent { get; set; }
    public string? TraceState { get; set; }
}
