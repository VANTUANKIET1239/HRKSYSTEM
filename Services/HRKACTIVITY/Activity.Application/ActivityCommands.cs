using Activity.Domain;
using Core.Messaging.Contracts;
using System.Text.Json;

namespace Activity.Application;

public sealed record RecordPlayerActivityCommand(PlayerActivityRecordedV1 Event, string MessageId,
    string? CorrelationId, string? TraceId);

public interface IActivityRecorder
{
    Task RecordAsync(RecordPlayerActivityCommand command, CancellationToken ct);
}

public sealed record GetActivitiesQuery(long? PlayerId = null, string? CorrelationId = null,
    string? EntityType = null, string? EntityId = null, int Page = 1, int PageSize = 50);

public interface IActivityQueries
{
    Task<IReadOnlyList<PlayerActivity>> GetAsync(GetActivitiesQuery query, CancellationToken ct);
    Task<PlayerActivity?> GetByIdAsync(Guid activityId, CancellationToken ct);
}

public static class ActivityMapping
{
    private static readonly HashSet<string> AllowedTypes = new(StringComparer.Ordinal)
    {
        "QuickClimbStarted", "QuickClimbCompleted", "QuickClimbFailed", "QuickClimbCancelled",
        "ItemEnhanced", "BattleCompleted", "PlayerLoggedIn", "RewardGranted", "CurrencyChanged",
        "ContentPublished", "ContentActivated", "AdminConfigurationChanged"
    };

    public static PlayerActivity Map(RecordPlayerActivityCommand command)
    {
        var message = command.Event;
        if (message.EventId == Guid.Empty || !Guid.TryParse(command.MessageId, out var id) || id != message.EventId)
            throw new ArgumentException("Activity event and transport MessageId must match.");
        if (!AllowedTypes.Contains(message.ActivityType)) throw new ArgumentException("Unsupported activity type.");
        Validate(message.EntityType, 100);
        Validate(message.EntityId, 200);
        Validate(message.SourceService, 100);
        if (message.UserId?.Length > 128 || message.PlayerId is <= 0 || message.OccurredAt == default ||
            command.CorrelationId?.Length > 128 || command.TraceId?.Length > 32)
            throw new ArgumentException("Invalid activity metadata.");
        if (message.Payload.ValueKind != JsonValueKind.Object || message.Payload.GetRawText().Length > 16384)
            throw new ArgumentException("Activity payload must be a bounded JSON object.");
        return new PlayerActivity
        {
            ActivityId = message.EventId, ActivityType = message.ActivityType, PlayerId = message.PlayerId,
            UserId = message.UserId, EntityType = message.EntityType, EntityId = message.EntityId,
            SourceService = message.SourceService, OccurredAt = message.OccurredAt.ToUniversalTime(),
            RecordedAt = DateTimeOffset.UtcNow, CorrelationId = command.CorrelationId,
            TraceId = command.TraceId, PayloadJson = message.Payload.GetRawText()
        };
    }

    private static void Validate(string value, int length)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > length)
            throw new ArgumentException("Invalid activity field length.");
    }
}
