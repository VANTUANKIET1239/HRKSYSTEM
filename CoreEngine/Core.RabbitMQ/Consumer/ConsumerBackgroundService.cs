using Core.RabbitMQ.Entities;
using Core.RabbitMQ.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace Hrk.Messaging.RabbitMq.Consuming;

/// <summary>
/// Generic async consumer (v7 API) with manual acks and retry→DLQ.
/// </summary>
public sealed class ConsumerBackgroundService<TMessage, THandler> : BackgroundService
    where THandler : class, IMessageHandler<TMessage>
{
    private readonly ILogger<ConsumerBackgroundService<TMessage, THandler>> _log;
    private readonly IRabbitMqChannel _channelFactory;
    private readonly RabbitMqOptions _opt;
    private readonly IServiceProvider _sp;
    private readonly string _queue;
    private IChannel? _channel;
    private string? _consumerTag;

    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    public ConsumerBackgroundService(
        ILogger<ConsumerBackgroundService<TMessage, THandler>> log,
        IRabbitMqChannel channelFactory,
        IOptions<RabbitMqOptions> options,
        IServiceProvider sp,
        string queue)
    {
        _log = log;
        _channelFactory = channelFactory;
        _opt = options.Value;
        _sp = sp;
        _queue = queue;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _channel = await _channelFactory.GetChannelAsync(stoppingToken);
        await _channel.BasicQosAsync(0, _opt.Consumer.PrefetchCount, global: false, cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (_, ea) =>
        {
            try
            {
                var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                var msg = JsonSerializer.Deserialize<TMessage>(json, JsonOpts)!;

                using var scope = _sp.CreateScope();
                var handler = scope.ServiceProvider.GetRequiredService<THandler>();
                await handler.HandleAsync(msg, stoppingToken);

                await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken: stoppingToken);
            }
            catch (OperationCanceledException)
            {
                try { await _channel!.BasicAckAsync(ea.DeliveryTag, false, stoppingToken); } catch { /* ignore */ }
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Handler failed — routing to retry (once) then DLQ.");

                var headers = ea.BasicProperties?.Headers ?? new Dictionary<string, object?>();
                var body = ea.Body.ToArray();

                if (!headers.ContainsKey("x-retried"))
                {
                    var props = new BasicProperties
                    {
                        Headers = new Dictionary<string, object?>(headers) { ["x-retried"] = 1 },
                        ContentType = ea.BasicProperties?.ContentType ?? "application/json",
                        DeliveryMode = DeliveryModes.Persistent
                    };

                    await _channel!.BasicPublishAsync(_opt.Consumer.RetryExchange, "retry", true, props, body, stoppingToken);
                    await _channel.BasicAckAsync(ea.DeliveryTag, false, stoppingToken);
                }
                else
                {
                    var props = new BasicProperties
                    {
                        ContentType = ea.BasicProperties?.ContentType ?? "application/json",
                        DeliveryMode = DeliveryModes.Persistent
                    };

                    await _channel!.BasicPublishAsync(_opt.Consumer.DlqExchange, "dlq", true, props, body, stoppingToken);
                    await _channel.BasicAckAsync(ea.DeliveryTag, false, stoppingToken);
                }
            }
        };

        _consumerTag = await _channel.BasicConsumeAsync(
            queue: _queue,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);

        _log.LogInformation("Started consumer on '{Queue}' (tag {Tag}).", _queue, _consumerTag);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (_channel is not null && !string.IsNullOrEmpty(_consumerTag))
                await _channel.BasicCancelAsync(_consumerTag,false, cancellationToken);
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Failed to cancel consumer cleanly.");
        }
        await base.StopAsync(cancellationToken);
    }

    public async override void Dispose()
    {
        try { if (_channel is not null) await _channel.DisposeAsync(); } catch { /* ignore */ }
        base.Dispose();
    }
}
