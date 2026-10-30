namespace Core.Scheduling;

public sealed class JobDefinition
{
    public long Id { get; set; }
    public string JobCode { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string HandlerKey { get; set; } = "";
    public string ScopeKey { get; set; } = "";
    public string CronExpression { get; set; } = "";
    public string TimeZoneId { get; set; } = "UTC";
    public string ParametersJson { get; set; } = "{}";
    public bool IsEnabled { get; set; } = true;
    public string MisfirePolicy { get; set; } = "FireOnceNow";
    public int MaxRetryAttempts { get; set; } = 2;
    public int RetryDelaySeconds { get; set; } = 5;
    public int TimeoutSeconds { get; set; } = 300;
    public int RecoveryIntervalSeconds { get; set; } = 300;
    public long ConfigVersion { get; set; } = 1;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public string? UpdatedBy { get; set; }
}
