namespace Core.Messaging.Contracts;

public static class AccountContractNames
{
    public const string RegisteredV1 = "auth.account.registered.v1";
    public const string Publisher = "AccountEvents";
    public const string GameAppCode = "dcs-game";
}

// Contains no credentials, tokens or email address.
public sealed record AccountRegisteredV1(Guid EventId, string UserId, string AppCode,
    string DisplayName, DateTimeOffset OccurredAt);
