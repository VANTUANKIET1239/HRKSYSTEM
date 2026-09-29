using System.Text;
using System.Text.Json;
using Core.RabbitMQ.Entities;
using Core.RabbitMQ.Interfaces;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace Core.RabbitMQ.Publisher;

public sealed class MessagePublisher : IMessagePublisher
{
    private readonly IRabbitMqChannel _channel;
    private readonly RabbitMqOptions _options;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public MessagePublisher(IOptions<RabbitMqOptions> options, IRabbitMqChannel channel)
    {
        _options = options.Value;
        _channel = channel;
    }

    public Task PublishJsonAsync<T>(T message, string? routingKey = null, CancellationToken ct = default)
    {
        return PublishAsync(_options.ResolvePublisher(), message, routingKey, ct);
    }

    public Task PublishJsonAsync<T>(
        string publisherName,
        T message,
        string? routingKey = null,
        CancellationToken ct = default)
    {
        return PublishAsync(_options.ResolvePublisher(publisherName), message, routingKey, ct);
    }

    private async Task PublishAsync<T>(
        RabbitMqOptions.PublisherOptions publisher,
        T message,
        string? routingKey,
        CancellationToken ct)
    {
        var channel = await _channel.GetChannelAsync(ct);
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message, JsonOptions));
        var properties = new BasicProperties
        {
            ContentType = "application/json",
            DeliveryMode = DeliveryModes.Persistent,
            MessageId = Guid.NewGuid().ToString("N"),
            Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds()),
            Type = typeof(T).FullName
        };

        await channel.BasicPublishAsync(
            exchange: publisher.Exchange,
            routingKey: routingKey ?? publisher.DefaultRoutingKey,
            mandatory: true,
            basicProperties: properties,
            body: body,
            cancellationToken: ct);
    }
}
