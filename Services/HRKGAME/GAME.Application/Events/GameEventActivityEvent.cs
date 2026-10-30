namespace GAME.Application.Events;

public sealed record GameEventActivityEvent(long PlayerId, string UserId, string ActivityType,
    string SourceId, object Metadata) : PlayerActivityEvent(PlayerId, UserId);
