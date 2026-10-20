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

public sealed class JobExecution
{
    public Guid Id { get; set; }
    public long JobDefinitionId { get; set; }
    public long ConfigVersion { get; set; }
    public string DefinitionSnapshotJson { get; set; } = "{}";
    public DateTime ScheduledAtUtc { get; set; }
    public DateTime StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public string Status { get; set; } = "RUNNING";
    public int Attempts { get; set; }
    public string? ResultJson { get; set; }
    public string? Error { get; set; }
    public string InstanceId { get; set; } = "";
}

public sealed record ScheduledJobContext(Guid ExecutionId, JobDefinition Definition, DateTime ScheduledAtUtc);
public sealed record ScheduledJobResult(int ProcessedCount = 0, string MetadataJson = "{}");

public interface IScheduledJobHandler
{
    string HandlerKey { get; }
    Task<ScheduledJobResult> ExecuteAsync(ScheduledJobContext context, CancellationToken ct);
}

public interface IJobScheduleManager
{
    Task SynchronizeAsync(CancellationToken ct = default);
    Task TriggerAsync(long definitionId, CancellationToken ct = default);
}

public sealed class SchedulingOptions
{
    public bool Enabled { get; set; }
    public string SchedulerName { get; set; } = "HRK";
    public string Group { get; set; } = "managed";
    public bool UsePersistentStore { get; set; } = true;
    public string? ConnectionString { get; set; }
    public int MaxConcurrency { get; set; } = 2;
    public int SynchronizationIntervalSeconds { get; set; } = 30;
}
