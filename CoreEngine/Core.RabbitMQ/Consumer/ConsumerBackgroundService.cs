using System.Text;
using System.Text.Json;
using System.Diagnostics;
using Oservability.Correlation;
using Oservability.Tracing;
using Serilog.Context;
using Core.RabbitMQ.Entities;
using Core.RabbitMQ.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Hrk.Messaging.RabbitMq.Consuming;

public sealed class ConsumerBackgroundService<TMessage, THandler> : BackgroundService
    where THandler : class, IMessageHandler<TMessage>
{
    private readonly ILogger<ConsumerBackgroundService<TMessage, THandler>> _logger;
    private readonly IRabbitMqChannel _channelFactory;
    private readonly RabbitMqOptions.ConsumerOptions _consumerOptions;
    private readonly IServiceProvider _serviceProvider;
    private readonly IMessageFailureClassifier _failureClassifier;
    private readonly string _consumerName;
    private IChannel? _channel;
    private string? _consumerTag;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public ConsumerBackgroundService(
        ILogger<ConsumerBackgroundService<TMessage, THandler>> logger,
        IRabbitMqChannel channelFactory,
        IOptions<RabbitMqOptions> options,
        IServiceProvider serviceProvider,
        IMessageFailureClassifier failureClassifier,
        string consumerName)
    {
        _logger = logger;
        _channelFactory = channelFactory;
        _consumerOptions = options.Value.ResolveConsumer(consumerName);
        _serviceProvider = serviceProvider;
        _failureClassifier = failureClassifier;
        _consumerName = consumerName;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Each consumer owns a channel. RabbitMQ channels must not be shared by
        // independent consumers or disposed by another hosted service.
        _channel = await _channelFactory.CreateChannelAsync(stoppingToken);
        await _channel.BasicQosAsync(
            0,
            _consumerOptions.PrefetchCount,
            global: false,
            cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (_, delivery) =>
        {
            using var activity = HrkTelemetry.Source.StartActivity("RabbitMQ Consume", ActivityKind.Consumer,
                HrkTelemetry.Extract(delivery.BasicProperties.Headers));
            activity?.SetTag("messaging.system", "rabbitmq");
            activity?.SetTag("messaging.destination.name", _consumerOptions.Queue);
            using var scope = _serviceProvider.CreateScope();
            var correlation = scope.ServiceProvider.GetService<CorrelationContext>();
            correlation?.Initialize(delivery.BasicProperties.CorrelationId);
            activity?.SetTag("correlation.id", correlation?.CorrelationId);
            var messageContext = scope.ServiceProvider.GetRequiredService<MessageContext>();
            messageContext.MessageId = delivery.BasicProperties.MessageId;
            messageContext.CorrelationId = correlation?.CorrelationId;
            messageContext.TraceId = Activity.Current?.TraceId.ToHexString();
            using var correlationScope = LogContext.PushProperty("CorrelationId", correlation?.CorrelationId);
            using var messageScope = LogContext.PushProperty("MessageId", delivery.BasicProperties.MessageId);
            using var consumerScope = LogContext.PushProperty("ConsumerName", _consumerName);
            var started = Stopwatch.GetTimestamp();
            try
            {
                var json = Encoding.UTF8.GetString(delivery.Body.ToArray());
                var message = JsonSerializer.Deserialize<TMessage>(json, JsonOptions)
                    ?? throw new JsonException($"Cannot deserialize {typeof(TMessage).Name}.");

                var handler = scope.ServiceProvider.GetRequiredService<THandler>();
                await handler.HandleAsync(message, stoppingToken);
                HrkTelemetry.Processed.Add(1, new KeyValuePair<string, object?>("consumer", _consumerName));
                _logger.LogDebug("Processed RabbitMQ message {MessageId}", delivery.BasicProperties.MessageId);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Host shutdown: leave the delivery unacked so RabbitMQ can redeliver it.
                return;
            }
            catch (Exception exception)
            {
                if (_failureClassifier.Classify(exception) != MessageFailureCategory.Duplicate)
                {
                    activity?.SetStatus(ActivityStatusCode.Error, exception.GetType().Name);
                    HrkTelemetry.Failed.Add(1, new KeyValuePair<string, object?>("consumer", _consumerName));
                }
                try
                {
                    await HandleFailureAsync(delivery, exception, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
                catch (Exception transferException)
                {
                    _logger.LogError(transferException,
                        "RabbitMQ consumer {ConsumerName} could not transfer or ACK failed message {MessageId}; original remains unacknowledged",
                        _consumerName, delivery.BasicProperties.MessageId);
                    // Release the delivery for recovery rather than permanently using a prefetch slot.
                    try { await _channel!.BasicNackAsync(delivery.DeliveryTag, false, true, stoppingToken); }
                    catch (Exception nackException) when (!stoppingToken.IsCancellationRequested)
                    {
                        _logger.LogWarning(nackException, "Failed to NACK message {MessageId}; channel recovery must redeliver it",
                            delivery.BasicProperties.MessageId);
                    }
                }
                return;
            }
            finally
            {
                HrkTelemetry.Duration.Record(Stopwatch.GetElapsedTime(started).TotalSeconds,
                    new KeyValuePair<string, object?>("consumer", _consumerName));
            }

            try
            {
                await _channel.BasicAckAsync(
                    delivery.DeliveryTag,
                    multiple: false,
                    cancellationToken: stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // The channel will redeliver the unacknowledged message after recovery.
            }
            catch (Exception exception)
            {
                // The handler already succeeded. Do not republish as a handler failure;
                // the unacked original can be redelivered and must be idempotent.
                _logger.LogWarning(
                    exception,
                    "RabbitMQ consumer {ConsumerName} could not ACK message {MessageId}; redelivery is expected.",
                    _consumerName,
                    delivery.BasicProperties?.MessageId);
            }
        };

        _consumerTag = await _channel.BasicConsumeAsync(
            queue: _consumerOptions.Queue,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);

        _logger.LogInformation(
            "Started RabbitMQ consumer {ConsumerName} on queue {Queue} with tag {Tag}.",
            _consumerName,
            _consumerOptions.Queue,
            _consumerTag);

        await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
    }

    private async Task HandleFailureAsync(
        BasicDeliverEventArgs delivery,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var category = _failureClassifier.Classify(exception);
        var headers = delivery.BasicProperties?.Headers is null
            ? new Dictionary<string, object?>()
            : new Dictionary<string, object?>(delivery.BasicProperties.Headers);
        var retryCount = ReadIntHeader(headers, "x-retry-count");

        if (category == MessageFailureCategory.Duplicate)
        {
            await AckAsync(delivery, cancellationToken);
            _logger.LogDebug("Acknowledged duplicate RabbitMQ message {MessageId}", delivery.BasicProperties?.MessageId);
            return;
        }

        var retryUnknown = string.Equals(
            _consumerOptions.Retry.UnknownExceptionBehavior,
            "Retry",
            StringComparison.OrdinalIgnoreCase);
        var shouldRetry = category is MessageFailureCategory.Transient or MessageFailureCategory.Cancelled ||
            (category == MessageFailureCategory.Unknown && retryUnknown);
        var delays = _consumerOptions.ResolveRetryDelays();

        headers.TryAdd("x-original-exchange", delivery.Exchange);
        headers.TryAdd("x-original-routing-key", delivery.RoutingKey);
        headers["x-consumer-name"] = _consumerName;
        headers["x-error-category"] = category.ToString();
        headers["x-error-type"] = exception.GetType().FullName ?? exception.GetType().Name;
        headers.Remove("x-error-message"); // Never transport raw exception messages/secrets.

        if (shouldRetry && retryCount < delays.Count)
        {
            headers["x-retry-count"] = retryCount + 1;
            var properties = CloneProperties(delivery.BasicProperties, headers);
            await _channel!.BasicPublishAsync(
                _consumerOptions.ResolveRetryExchange(),
                _consumerOptions.ResolveRetryBucketRoutingKey(retryCount),
                mandatory: true,
                basicProperties: properties,
                body: delivery.Body,
                cancellationToken: cancellationToken);
            _logger.LogWarning(exception,
                "Scheduled retry {Attempt}/{MaxAttempts} in {RetryDelayMs} ms for message {MessageId}; category {Category}",
                retryCount + 1, delays.Count, delays[retryCount], delivery.BasicProperties?.MessageId, category);
            await AckAsync(delivery, cancellationToken);
            return;
        }

        if (_consumerOptions.DeadLetter.Enabled)
        {
            var properties = CloneProperties(delivery.BasicProperties, headers);
            await _channel!.BasicPublishAsync(
                _consumerOptions.ResolveDlqExchange(),
                "dlq",
                mandatory: true,
                basicProperties: properties,
                body: delivery.Body,
                cancellationToken: cancellationToken);
            _logger.LogError(exception,
                "Moved failed message {MessageId} to {DlqQueue} after {RetryCount} retries; category {Category}",
                delivery.BasicProperties?.MessageId, _consumerOptions.ResolveDlqQueue(), retryCount, category);
            await AckAsync(delivery, cancellationToken);
            return;
        }

        await _channel!.BasicNackAsync(
            delivery.DeliveryTag,
            multiple: false,
            requeue: false,
            cancellationToken: cancellationToken);
        _logger.LogError(exception, "Rejected message {MessageId} without requeue; category {Category}",
            delivery.BasicProperties?.MessageId, category);
    }

    private Task AckAsync(BasicDeliverEventArgs delivery, CancellationToken ct) =>
        _channel!.BasicAckAsync(delivery.DeliveryTag, multiple: false, cancellationToken: ct).AsTask();

    private static BasicProperties CloneProperties(
        IReadOnlyBasicProperties? source,
        IDictionary<string, object?> headers) =>
        new()
        {
            Headers = headers,
            ContentType = source?.ContentType ?? "application/json",
            ContentEncoding = source?.ContentEncoding,
            DeliveryMode = DeliveryModes.Persistent,
            MessageId = source?.MessageId,
            CorrelationId = source?.CorrelationId,
            ReplyTo = source?.ReplyTo,
            Type = source?.Type,
            UserId = source?.UserId,
            AppId = source?.AppId,
            Timestamp = source?.Timestamp ?? new AmqpTimestamp(0)
        };

    private static int ReadIntHeader(IReadOnlyDictionary<string, object?> headers, string key)
    {
        if (!headers.TryGetValue(key, out var value) || value is null)
        {
            return 0;
        }

        return value switch
        {
            int number => number,
            long number when number is >= 0 and <= int.MaxValue => (int)number,
            byte number => number,
            byte[] bytes when int.TryParse(Encoding.UTF8.GetString(bytes), out var number) => number,
            _ when int.TryParse(value.ToString(), out var number) => number,
            _ => 0
        };
    }

    private static string Truncate(string value, int maxLength) =>
        value.Length <= Math.Max(1, maxLength)
            ? value
            : value[..Math.Max(1, maxLength)];

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (_channel is not null && !string.IsNullOrWhiteSpace(_consumerTag))
            {
                await _channel.BasicCancelAsync(_consumerTag, false, cancellationToken);
            }
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "Failed to stop RabbitMQ consumer {ConsumerName} cleanly.",
                _consumerName);
        }

        await base.StopAsync(cancellationToken);
    }

    public override void Dispose()
    {
        if (_channel is not null)
        {
            _channel.Dispose();
        }

        base.Dispose();
    }
}
