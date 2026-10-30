namespace GAME.Application.Events;

public sealed record CheckInClaimedEvent(long PlayerId, string UserId, long CalendarId, string MonthKey,
    DateOnly CheckInDate, Guid RequestId, int ConfigVersion, object Rewards) : PlayerActivityEvent(PlayerId, UserId);
