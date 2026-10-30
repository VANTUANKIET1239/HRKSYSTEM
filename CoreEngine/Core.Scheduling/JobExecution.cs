namespace Core.Scheduling;

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
