using System.Text.Json;
using System.Diagnostics;
using Oservability.Correlation;
using Core.TransactionalMessaging.Entities;
using Microsoft.EntityFrameworkCore;

namespace Core.TransactionalMessaging.Outbox;

public sealed class EfOutboxWriter<TDbContext>(TDbContext db, ICorrelationContext? correlation = null) : IOutboxWriter
    where TDbContext : DbContext
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public OutboxMessage Add<T>(
        T message,
        string eventName,
        string publisherName,
        string routingKey,
        int eventVersion = 1,
        string? partitionKey = null,
        long? sequence = null)
    {
        ArgumentNullException.ThrowIfNull(message);

        var eventId = TryGetEventId(message) ?? Guid.NewGuid();
        var outbox = new OutboxMessage
        {
            Id = eventId,
            EventName = eventName,
            EventVersion = eventVersion,
            PublisherName = publisherName,
            RoutingKey = routingKey,
            Payload = JsonSerializer.Serialize(message, JsonOptions),
            Status = OutboxStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            PartitionKey = partitionKey,
            Sequence = sequence,
            CorrelationId = correlation?.CorrelationId,
            TraceParent = Activity.Current?.Id,
            TraceState = Activity.Current?.TraceStateString
        };

        db.Set<OutboxMessage>().Add(outbox);
        return outbox;
    }

    private static Guid? TryGetEventId<T>(T message)
    {
        var property = typeof(T).GetProperty("EventId");
        return property?.GetValue(message) is Guid value && value != Guid.Empty ? value : null;
    }
}
