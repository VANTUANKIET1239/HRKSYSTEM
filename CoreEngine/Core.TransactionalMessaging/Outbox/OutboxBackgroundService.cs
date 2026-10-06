using Core.RabbitMQ.Entities;
using Core.RabbitMQ.Interfaces;
using Core.TransactionalMessaging.Configuration;
using Core.TransactionalMessaging.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using Oservability.Tracing;
using Serilog.Context;

namespace Core.TransactionalMessaging.Outbox;

public sealed class OutboxBackgroundService(
    IServiceScopeFactory scopeFactory,
    IMessagePublisher publisher,
    IMessageFailureClassifier failureClassifier,
    IOptions<TransactionalMessagingOptions> options,
    ILogger<OutboxBackgroundService> logger) : BackgroundService
{
    private readonly OutboxOptions _options = options.Value.Outbox;
    private readonly string _workerId = $"outbox-{Environment.MachineName}-{Guid.NewGuid():N}";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            logger.LogInformation("Outbox publisher is disabled.");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var didWork = await ProcessBatchAsync(stoppingToken);
                if (!didWork)
                {
                    await Task.Delay(Math.Max(1, _options.PollingIntervalMs), stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Outbox worker {WorkerId} failed its polling cycle.", _workerId);
                await Task.Delay(Math.Max(1, _options.PollingIntervalMs), stoppingToken);
            }
        }
    }

    private async Task<bool> ProcessBatchAsync(CancellationToken stoppingToken)
    {
        var lockToken = Guid.NewGuid().ToString("N");
        IReadOnlyList<OutboxMessage> messages;
        using (var scope = scopeFactory.CreateScope())
        {
            var store = scope.ServiceProvider.GetRequiredService<IOutboxStore>();
            var batchSize = Math.Max(1, _options.BatchSize);
            var leaseSeconds = Math.Max(
                Math.Max(1, _options.LeaseSeconds),
                checked(batchSize * Math.Max(1, _options.PublishTimeoutSeconds) + 10));
            messages = await store.ClaimAsync(
                batchSize,
                lockToken,
                leaseSeconds,
                stoppingToken);
        }

        foreach (var message in messages)
        {
            // A persisted request context, not the polling cycle, is the parent.
            using var activity = HrkTelemetry.Source.StartActivity("Outbox Dispatch", ActivityKind.Internal,
                HrkTelemetry.Extract(message.TraceParent, message.TraceState));
            using var correlationScope = LogContext.PushProperty("CorrelationId", message.CorrelationId);
            using var messageScope = LogContext.PushProperty("MessageId", message.Id.ToString("N"));
            try
            {
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                timeout.CancelAfter(TimeSpan.FromSeconds(Math.Max(1, _options.PublishTimeoutSeconds)));
                await publisher.PublishRawJsonAsync(
                    message.PublisherName,
                    message.Payload,
                    message.RoutingKey,
                    new RabbitPublishMetadata
                    {
                        MessageId = message.Id.ToString("N"),
                        CorrelationId = message.CorrelationId,
                        EventName = message.EventName,
                        EventVersion = message.EventVersion,
                        Headers = message.Sequence.HasValue
                            ? new Dictionary<string, object?> { ["x-sequence"] = message.Sequence.Value }
                            : null
                    },
                    timeout.Token);

                using var scope = scopeFactory.CreateScope();
                var store = scope.ServiceProvider.GetRequiredService<IOutboxStore>();
                var affected = await store.MarkPublishedAsync(message.Id, lockToken, stoppingToken);
                if (affected == 0)
                {
                    logger.LogWarning(
                        "Outbox {OutboxId} was published but ownership {LockToken} was lost before completion.",
                        message.Id,
                        lockToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                activity?.SetStatus(ActivityStatusCode.Error, exception.GetType().Name);
                var permanent = failureClassifier.Classify(exception) == MessageFailureCategory.Permanent ||
                    exception is ArgumentException or InvalidOperationException;
                using var scope = scopeFactory.CreateScope();
                var store = scope.ServiceProvider.GetRequiredService<IOutboxStore>();
                var affected = await store.MarkFailedAttemptAsync(
                    message,
                    lockToken,
                    _options,
                    exception,
                    permanent,
                    CancellationToken.None);
                if (affected == 0)
                {
                    logger.LogWarning(
                        "Outbox {OutboxId} failure was not recorded because ownership {LockToken} was lost.",
                        message.Id,
                        lockToken);
                }
                else
                {
                    logger.Log(permanent || message.RetryCount + 1 >= Math.Max(1, _options.MaxRetryCount)
                            ? LogLevel.Error : LogLevel.Warning,
                        exception,
                        "Outbox {OutboxId} publish attempt {Attempt} failed.",
                        message.Id,
                        message.RetryCount + 1);
                }
            }
        }

        return messages.Count > 0;
    }
}
