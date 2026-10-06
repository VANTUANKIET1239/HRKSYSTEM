using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.RabbitMQ.Interfaces
{
    public interface IMessagePublisher
    {
        Task PublishJsonAsync<T>(T message, string? routingKey = null, CancellationToken ct = default);

        Task PublishJsonAsync<T>(
            string publisherName,
            T message,
            string? routingKey = null,
            CancellationToken ct = default);

        Task PublishJsonAsync<T>(
            string publisherName,
            T message,
            string? routingKey,
            Core.RabbitMQ.Entities.RabbitPublishMetadata? metadata,
            CancellationToken ct = default);

        Task PublishRawJsonAsync(
            string publisherName,
            string json,
            string routingKey,
            Core.RabbitMQ.Entities.RabbitPublishMetadata metadata,
            CancellationToken ct = default);
    }
}
