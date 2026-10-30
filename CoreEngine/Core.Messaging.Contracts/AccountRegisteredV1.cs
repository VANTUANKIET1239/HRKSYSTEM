namespace Core.Messaging.Contracts;

// Contains no credentials, tokens or email address.
public sealed record AccountRegisteredV1(Guid EventId, string UserId, string AppCode,
    string DisplayName, DateTimeOffset OccurredAt);
