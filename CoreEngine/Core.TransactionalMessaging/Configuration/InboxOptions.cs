namespace Core.TransactionalMessaging.Configuration;

public sealed class InboxOptions
{
    public bool CleanupEnabled { get; set; } = true;
    public int RetentionDays { get; set; } = 30;
    public int CleanupBatchSize { get; set; } = 1000;
    public int CleanupIntervalMinutes { get; set; } = 60;
}
