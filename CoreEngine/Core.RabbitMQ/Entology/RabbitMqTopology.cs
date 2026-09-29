using Core.RabbitMQ.Entities;
using Core.RabbitMQ.Interfaces;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace Core.RabbitMQ.Entology;

public sealed class RabbitMqTopology : IRabbitMqTopology
{
    private readonly RabbitMqOptions _options;
    private readonly IRabbitMqChannel _channel;

    public RabbitMqTopology(IOptions<RabbitMqOptions> options, IRabbitMqChannel channel)
    {
        _options = options.Value;
        _channel = channel;
    }

    public async Task EnsureAsync(CancellationToken ct = default)
    {
        var channel = await _channel.GetChannelAsync(ct);

        foreach (var (_, publisher) in _options.GetPublishers())
        {
            await channel.ExchangeDeclareAsync(
                publisher.Exchange,
                publisher.ExchangeType,
                durable: publisher.Durable,
                autoDelete: false,
                cancellationToken: ct);
        }

        foreach (var (_, consumer) in _options.GetConsumers())
        {
            await EnsureConsumerAsync(channel, consumer, ct);
        }
    }

    private async Task EnsureConsumerAsync(
        IChannel channel,
        RabbitMqOptions.ConsumerOptions consumer,
        CancellationToken ct)
    {
        var legacyPublisher = _options.Publisher;
        var exchange = consumer.ResolveExchange(legacyPublisher);
        var routingKeys = consumer.ResolveRoutingKeys(legacyPublisher);
        var retryExchange = consumer.ResolveRetryExchange();
        var retryQueue = consumer.ResolveRetryQueue();
        var dlqExchange = consumer.ResolveDlqExchange();
        var dlqQueue = consumer.ResolveDlqQueue();

        await channel.ExchangeDeclareAsync(
            exchange,
            consumer.ExchangeType,
            durable: consumer.ExchangeDurable,
            autoDelete: false,
            cancellationToken: ct);

        await channel.QueueDeclareAsync(
            consumer.Queue,
            durable: consumer.QueueDurable,
            exclusive: false,
            autoDelete: false,
            cancellationToken: ct);

        foreach (var routingKey in routingKeys.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            await channel.QueueBindAsync(
                consumer.Queue,
                exchange,
                routingKey,
                cancellationToken: ct);
        }

        await channel.ExchangeDeclareAsync(
            dlqExchange,
            ExchangeType.Direct,
            durable: true,
            autoDelete: false,
            cancellationToken: ct);
        await channel.QueueDeclareAsync(
            dlqQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: ct);
        await channel.QueueBindAsync(
            dlqQueue,
            dlqExchange,
            routingKey: "dlq",
            cancellationToken: ct);

        await channel.ExchangeDeclareAsync(
            retryExchange,
            ExchangeType.Direct,
            durable: true,
            autoDelete: false,
            cancellationToken: ct);
        await channel.QueueDeclareAsync(
            retryQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: new Dictionary<string, object?>
            {
                ["x-message-ttl"] = consumer.RetryDelayMs,
                ["x-dead-letter-exchange"] = exchange
            },
            cancellationToken: ct);

        foreach (var routingKey in routingKeys.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            await channel.QueueBindAsync(
                retryQueue,
                retryExchange,
                routingKey,
                cancellationToken: ct);
        }
    }
}
