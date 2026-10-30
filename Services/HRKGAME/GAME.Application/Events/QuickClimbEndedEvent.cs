namespace GAME.Application.Events;

public sealed record QuickClimbEndedEvent(
    long PlayerId, string UserId, string JobId, string Status, int StartFloor,
    int CurrentFloor, int TargetFloor, int ClearedFloorsCount, string? StopReason,
    long Version) : PlayerActivityEvent(PlayerId, UserId);
