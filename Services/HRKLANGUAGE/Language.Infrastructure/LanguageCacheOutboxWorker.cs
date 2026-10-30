using System.Text.Json;
using Core.Localization;
using Core.TransactionalMessaging.Configuration;
using Core.TransactionalMessaging.Outbox;
using Language.Application;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Language.Infrastructure;

// Dedicated destination for this service's outbox: writes the shared cache, no per-instance broadcast.
public sealed partial class LanguageCacheOutboxWorker(IServiceScopeFactory scopes,
    IOptions<TransactionalMessagingOptions> options, ILogger<LanguageCacheOutboxWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var settings = options.Value.Outbox;
        if (!settings.Enabled) return;
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopes.CreateScope();
                var store = scope.ServiceProvider.GetRequiredService<IOutboxStore>();
                var token = Guid.NewGuid().ToString("N");
                // Claim a single row so a Redis timeout cannot exhaust a following row's lease.
                var messages = await store.ClaimAsync(1, token, Math.Max(30, settings.LeaseSeconds), stoppingToken);
                foreach (var message in messages)
                {
                    try
                    {
                        var identity = JsonSerializer.Deserialize<CachePublication>(message.Payload,
                            new JsonSerializerOptions(JsonSerializerDefaults.Web))
                            ?? throw new InvalidOperationException("Missing language publication identity.");
                        // Read the CURRENT DB publication, even if this outbox event is delayed or replayed.
                        var bundle = await scope.ServiceProvider.GetRequiredService<IPublishedLanguageReader>()
                            .GetFromDatabaseAsync(identity.Group, identity.Culture, stoppingToken)
                            ?? throw new InvalidOperationException("Published language bundle does not exist.");
                        await scope.ServiceProvider.GetRequiredService<ITranslationCache>().PublishAsync(bundle, stoppingToken);
                        await store.MarkPublishedAsync(message.Id, token, stoppingToken);
                    }
                    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { throw; }
                    catch (Exception ex)
                    {
                        await store.MarkFailedAttemptAsync(message, token, settings, ex,
                            ex is JsonException or InvalidOperationException, stoppingToken);
                        logger.LogWarning(ex, "Language cache outbox {MessageId} failed.", message.Id);
                    }
                }
                if (messages.Count == 0) await Task.Delay(Math.Max(100, settings.PollingIntervalMs), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex)
            {
                logger.LogError(ex, "Language cache synchronization failed.");
                await Task.Delay(Math.Max(100, settings.PollingIntervalMs), stoppingToken);
            }
        }
    }
}
