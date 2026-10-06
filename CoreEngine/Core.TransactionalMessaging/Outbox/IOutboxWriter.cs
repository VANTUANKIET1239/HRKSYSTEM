using Core.TransactionalMessaging.Entities;

namespace Core.TransactionalMessaging.Outbox;

public interface IOutboxWriter
{
    OutboxMessage Add<T>(
        T message,
        string eventName,
        string publisherName,
        string routingKey,
        int eventVersion = 1,
        string? partitionKey = null,
        long? sequence = null);
}
