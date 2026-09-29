using System.Text;
using System.Text.Json;
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
    private readonly string _consumerName;
    private IChannel? _channel;
    private string? _consumerTag;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public ConsumerBackgroundService(
        ILogger<ConsumerBackgroundService<TMessage, THandler>> logger,
        IRabbitMqChannel channelFactory,
        IOptions<RabbitMqOptions> options,
        IServiceProvider serviceProvider,
        string consumerName)
    {
        _logger = logger;
        _channelFactory = channelFactory;
        _consumerOptions = options.Value.ResolveConsumer(consumerName);
        _serviceProvider = serviceProvider;
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
            try
            {
                var json = Encoding.UTF8.GetString(delivery.Body.ToArray());
                var message = JsonSerializer.Deserialize<TMessage>(json, JsonOptions)
                    ?? throw new JsonException($"Cannot deserialize {typeof(TMessage).Name}.");

                using var scope = _serviceProvider.CreateScope();
                var handler = scope.ServiceProvider.GetRequiredService<THandler>();
                await handler.HandleAsync(message, stoppingToken);

                await _channel.BasicAckAsync(
                    delivery.DeliveryTag,
                    multiple: false,
                    cancellationToken: stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Host shutdown: leave the delivery unacked so RabbitMQ can redeliver it.
            }
            catch (Exception exception)
            {
                await HandleFailureAsync(delivery, exception, stoppingToken);
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
    }

    private async Task HandleFailureAsync(
        BasicDeliverEventArgs delivery,
        Exception exception,
        CancellationToken cancellationToken)
    {
        _logger.LogError(
            exception,
            "RabbitMQ consumer {ConsumerName} failed to handle message {MessageId}.",
            _consumerName,
            delivery.BasicProperties?.MessageId);

        var existingHeaders = delivery.BasicProperties?.Headers
            ?? new Dictionary<string, object?>();
        var wasRetried = existingHeaders.ContainsKey("x-retried");
        var properties = new BasicProperties
        {
            Headers = new Dictionary<string, object?>(existingHeaders),
            ContentType = delivery.BasicProperties?.ContentType ?? "application/json",
            DeliveryMode = DeliveryModes.Persistent,
            MessageId = delivery.BasicProperties?.MessageId,
            Type = delivery.BasicProperties?.Type,
            Timestamp = delivery.BasicProperties?.Timestamp ?? new AmqpTimestamp(0)
        };

        if (!wasRetried)
        {
            properties.Headers["x-retried"] = 1;
            await _channel!.BasicPublishAsync(
                _consumerOptions.ResolveRetryExchange(),
                delivery.RoutingKey,
                mandatory: true,
                basicProperties: properties,
                body: delivery.Body,
                cancellationToken: cancellationToken);
        }
        else
        {
            await _channel!.BasicPublishAsync(
                _consumerOptions.ResolveDlqExchange(),
                "dlq",
                mandatory: true,
                basicProperties: properties,
                body: delivery.Body,
                cancellationToken: cancellationToken);
        }

        await _channel!.BasicAckAsync(
            delivery.DeliveryTag,
            multiple: false,
            cancellationToken: cancellationToken);
    }

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
