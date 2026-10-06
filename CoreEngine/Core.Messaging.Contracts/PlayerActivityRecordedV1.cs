using System.Text.Json;

namespace Core.Messaging.Contracts;

public static class ActivityContractNames
{
    public const string PlayerActivityRecordedV1 = "game.activity.recorded.v1";
}

// A summary event. Owning histories/replays remain in the source service.
public sealed record PlayerActivityRecordedV1(
    Guid EventId,
    string ActivityType,
    long? PlayerId,
    string? UserId,
    string EntityType,
    string EntityId,
    string SourceService,
    DateTimeOffset OccurredAt,
    JsonElement Payload);
