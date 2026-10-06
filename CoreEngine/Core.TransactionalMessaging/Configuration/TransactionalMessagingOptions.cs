namespace Core.TransactionalMessaging.Configuration;

public sealed class TransactionalMessagingOptions
{
    public const string SectionName = "TransactionalMessaging";

    public OutboxOptions Outbox { get; set; } = new();
    public InboxOptions Inbox { get; set; } = new();
}

public sealed class OutboxOptions
{
    public bool Enabled { get; set; } = true;
    public int BatchSize { get; set; } = 20;
    public int PollingIntervalMs { get; set; } = 1000;
    public int LeaseSeconds { get; set; } = 60;
    public int PublishTimeoutSeconds { get; set; } = 15;
    public int MaxRetryCount { get; set; } = 10;
    public List<int> RetryDelaysMs { get; set; } = [1000, 5000, 15000, 30000, 60000, 120000];
    public int ErrorMaxLength { get; set; } = 2000;
}

public sealed class InboxOptions
{
    public bool CleanupEnabled { get; set; } = true;
    public int RetentionDays { get; set; } = 30;
    public int CleanupBatchSize { get; set; } = 1000;
    public int CleanupIntervalMinutes { get; set; } = 60;
}
