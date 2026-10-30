namespace GAME.Application.Events;

public abstract record PlayerActivityEvent(long PlayerId, string UserId)
{
    public int? GameEventId { get; init; }
    public long? EventPeriodId { get; init; }
    public string? PeriodKey { get; init; }
    public Guid? JobExecutionId { get; init; }
    public DateTimeOffset? OccurredAt { get; init; }
}
