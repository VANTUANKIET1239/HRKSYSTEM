namespace Core.TransactionalMessaging.Configuration;

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
