namespace Core.Messaging.Contracts;

public static class MessagingContractNames
{
    public const string QuickClimbFloorRequestedV1 = "game.tower.quick-climb.floor.requested.v1";
    public const string ProcessStatusUpdatedV1 = "game.process.status.updated.v1";
}

public static class MessagingPublisherNames
{
    public const string GameEvents = "GameEvents";
}

public sealed record ProcessQuickClimbFloorRequestedV1(
    Guid EventId,
    string JobId,
    string UserId,
    int ExpectedFloor,
    long Version);

public sealed record ProcessStatusUpdatedV1(
    Guid EventId,
    string JobId,
    string UserId,
    string ProcessType,
    long Version,
    int Current,
    int Total,
    int Percentage,
    string Status,
    DateTime OccurredAt,
    string? ErrorCode = null,
    string? ErrorMessage = null);
