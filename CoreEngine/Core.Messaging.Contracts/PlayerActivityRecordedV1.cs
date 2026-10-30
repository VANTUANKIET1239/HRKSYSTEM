using System.Text.Json;

namespace Core.Messaging.Contracts;

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
    JsonElement Payload,
    int? GameEventId = null,
    long? EventPeriodId = null,
    string? PeriodKey = null,
    Guid? JobExecutionId = null);
