using Activity.Domain;
using Core.Messaging.Contracts;
using System.Text.Json;

namespace Activity.Application;

public sealed record RecordPlayerActivityCommand(PlayerActivityRecordedV1 Event, string MessageId,
    string? CorrelationId, string? TraceId);
