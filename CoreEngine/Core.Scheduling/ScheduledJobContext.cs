namespace Core.Scheduling;

public sealed record ScheduledJobContext(Guid ExecutionId, JobDefinition Definition, DateTime ScheduledAtUtc);
