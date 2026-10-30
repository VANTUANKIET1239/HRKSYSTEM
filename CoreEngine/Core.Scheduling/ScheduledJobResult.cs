namespace Core.Scheduling;
public sealed record ScheduledJobResult(int ProcessedCount = 0, string MetadataJson = "{}");
