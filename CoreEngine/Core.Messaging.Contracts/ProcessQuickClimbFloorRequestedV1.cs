namespace Core.Messaging.Contracts;

public sealed record ProcessQuickClimbFloorRequestedV1(
    Guid EventId,
    string JobId,
    string UserId,
    int ExpectedFloor,
    long Version);
