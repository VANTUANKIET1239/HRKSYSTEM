namespace Activity.Domain;

public sealed class PlayerActivity
{
    public long Id { get; set; }
    public Guid ActivityId { get; set; }
    public string ActivityType { get; set; } = "";
    public long? PlayerId { get; set; }
    public string? UserId { get; set; }
    public string EntityType { get; set; } = "";
    public string EntityId { get; set; } = "";
    public string? CorrelationId { get; set; }
    public string? TraceId { get; set; }
    public string SourceService { get; set; } = "";
    public string PayloadJson { get; set; } = "{}";
    public DateTimeOffset OccurredAt { get; set; }
    public DateTimeOffset RecordedAt { get; set; }
    public int? GameEventId { get; set; }
    public long? EventPeriodId { get; set; }
    public string? PeriodKey { get; set; }
    public Guid? JobExecutionId { get; set; }
}
