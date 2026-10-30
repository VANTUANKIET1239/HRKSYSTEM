namespace GAME.Application.Events;

public sealed record QuickClimbStartedEvent(
    long PlayerId, string UserId, string JobId, int StartFloor, int CurrentFloor,
    int TargetFloor, int ClearedFloorsCount, long Version) : PlayerActivityEvent(PlayerId, UserId);
