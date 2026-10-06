using System.Text;
using System.Text.Json;
using System.Diagnostics;
using Oservability.Correlation;
using Oservability.Tracing;
using Microsoft.Extensions.Logging;
using Serilog.Context;
using Microsoft.AspNetCore.Http;
using Core.RabbitMQ.Entities;
using Core.RabbitMQ.Interfaces;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace Core.RabbitMQ.Publisher;

public sealed class MessagePublisher : IMessagePublisher
{
    private readonly IRabbitMqChannel _channel;
    private readonly RabbitMqOptions _options;
    private readonly SemaphoreSlim _publishGate = new(1, 1);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly ILogger<MessagePublisher> _logger;
    private readonly IHttpContextAccessor _httpContext;

    public MessagePublisher(IOptions<RabbitMqOptions> options, IRabbitMqChannel channel,
        ILogger<MessagePublisher> logger, IHttpContextAccessor httpContext)
    {
        _options = options.Value;
        _channel = channel;
        _logger = logger;
        _httpContext = httpContext;
    }

    public Task PublishJsonAsync<T>(T message, string? routingKey = null, CancellationToken ct = default)
    {
        return PublishAsync(_options.ResolvePublisher(), message, routingKey, null, ct);
    }

    public Task PublishJsonAsync<T>(
        string publisherName,
        T message,
        string? routingKey = null,
        CancellationToken ct = default)
    {
        return PublishAsync(_options.ResolvePublisher(publisherName), message, routingKey, null, ct);
    }

    public Task PublishJsonAsync<T>(
        string publisherName,
        T message,
        string? routingKey,
        RabbitPublishMetadata? metadata,
        CancellationToken ct = default)
    {
        return PublishAsync(_options.ResolvePublisher(publisherName), message, routingKey, metadata, ct);
    }

    public Task PublishRawJsonAsync(
        string publisherName,
        string json,
        string routingKey,
        RabbitPublishMetadata metadata,
        CancellationToken ct = default)
    {
        return PublishBytesAsync(
            _options.ResolvePublisher(publisherName),
            Encoding.UTF8.GetBytes(json),
            routingKey,
            metadata,
            ct);
    }

    private async Task PublishAsync<T>(
        RabbitMqOptions.PublisherOptions publisher,
        T message,
        string? routingKey,
        RabbitPublishMetadata? metadata,
        CancellationToken ct)
    {
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message, JsonOptions));
        metadata ??= new RabbitPublishMetadata
        {
            MessageId = Guid.NewGuid().ToString("N"),
            EventName = typeof(T).FullName
        };

        await PublishBytesAsync(
            publisher,
            body,
            routingKey ?? publisher.DefaultRoutingKey,
            metadata,
            ct);
    }

    private async Task PublishBytesAsync(
        RabbitMqOptions.PublisherOptions publisher,
        ReadOnlyMemory<byte> body,
        string routingKey,
        RabbitPublishMetadata metadata,
        CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(metadata.MessageId);
        var correlationId = metadata.CorrelationId ??
            _httpContext.HttpContext?.Items[CorrelationContext.HeaderName] as string ??
            Activity.Current?.GetTagItem("correlation.id") as string;
        using var activity = HrkTelemetry.Source.StartActivity("RabbitMQ Publish", ActivityKind.Producer);
        activity?.SetTag("messaging.system", "rabbitmq");
        activity?.SetTag("messaging.destination.name", publisher.Exchange);
        using var messageScope = LogContext.PushProperty("MessageId", metadata.MessageId);
        await _publishGate.WaitAsync(ct);
        try
        {
            var channel = await _channel.GetChannelAsync(ct);
            var headers = metadata.Headers is null
                ? new Dictionary<string, object?>()
                : new Dictionary<string, object?>(metadata.Headers);
            headers["x-event-version"] = metadata.EventVersion;
            HrkTelemetry.Inject(headers);

            var properties = new BasicProperties
            {
                ContentType = "application/json",
                DeliveryMode = DeliveryModes.Persistent,
                MessageId = metadata.MessageId,
                CorrelationId = correlationId,
                Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds()),
                Type = metadata.EventName,
                Headers = headers
            };

            await channel.BasicPublishAsync(
                exchange: publisher.Exchange,
                routingKey: routingKey,
                mandatory: true,
                basicProperties: properties,
                body: body,
                cancellationToken: ct);
            _logger.LogDebug("Published RabbitMQ message {MessageId} to {Exchange} with event {EventType}",
                metadata.MessageId, publisher.Exchange, metadata.EventName);
        }
        catch (Exception exception)
        {
            activity?.SetStatus(ActivityStatusCode.Error, exception.GetType().Name);
            // The caller decides whether this failure is retryable and logs it once.
            throw;
        }
        finally
        {
            _publishGate.Release();
        }
    }
}
