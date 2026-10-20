namespace GAME.Application.Events;

public abstract record PlayerActivityEvent(long PlayerId, string UserId)
{
    public int? GameEventId { get; init; }
    public long? EventPeriodId { get; init; }
    public string? PeriodKey { get; init; }
    public Guid? JobExecutionId { get; init; }
    public DateTimeOffset? OccurredAt { get; init; }
}

public sealed record GameEventActivityEvent(long PlayerId, string UserId, string ActivityType,
    string SourceId, object Metadata) : PlayerActivityEvent(PlayerId, UserId);

public sealed record CheckInClaimedEvent(long PlayerId, string UserId, long CalendarId, string MonthKey,
    DateOnly CheckInDate, Guid RequestId, int ConfigVersion, object Rewards) : PlayerActivityEvent(PlayerId, UserId);

public sealed record ItemEnhancedEvent(
    long PlayerId, string UserId, Guid RequestId, long ItemId, long ItemTemplateId,
    int OldLevel, int NewLevel, bool Success, long GoldCost) : PlayerActivityEvent(PlayerId, UserId);

public sealed record QuickClimbStartedEvent(
    long PlayerId, string UserId, string JobId, int StartFloor, int CurrentFloor,
    int TargetFloor, int ClearedFloorsCount, long Version) : PlayerActivityEvent(PlayerId, UserId);

public sealed record QuickClimbEndedEvent(
    long PlayerId, string UserId, string JobId, string Status, int StartFloor,
    int CurrentFloor, int TargetFloor, int ClearedFloorsCount, string? StopReason,
    long Version) : PlayerActivityEvent(PlayerId, UserId);
