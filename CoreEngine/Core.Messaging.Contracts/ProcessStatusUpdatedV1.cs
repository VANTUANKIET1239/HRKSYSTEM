namespace Core.Messaging.Contracts;

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
