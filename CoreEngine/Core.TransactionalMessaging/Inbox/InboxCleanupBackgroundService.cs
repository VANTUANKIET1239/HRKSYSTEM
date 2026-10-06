using Core.TransactionalMessaging.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Core.TransactionalMessaging.Inbox;

public sealed class InboxCleanupBackgroundService(
    IServiceScopeFactory scopeFactory,
    IOptions<TransactionalMessagingOptions> options,
    ILogger<InboxCleanupBackgroundService> logger) : BackgroundService
{
    private readonly InboxOptions _options = options.Value.Inbox;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.CleanupEnabled)
        {
            logger.LogInformation("Inbox cleanup is disabled.");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var store = scope.ServiceProvider.GetRequiredService<IInboxStore>();
                var olderThanUtc = DateTime.UtcNow.AddDays(-Math.Max(1, _options.RetentionDays));
                var deleted = await store.DeleteExpiredAsync(
                    olderThanUtc,
                    Math.Max(1, _options.CleanupBatchSize),
                    stoppingToken);

                if (deleted > 0)
                {
                    logger.LogInformation("Deleted {Count} expired Inbox messages.", deleted);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Inbox cleanup cycle failed.");
            }

            await Task.Delay(
                TimeSpan.FromMinutes(Math.Max(1, _options.CleanupIntervalMinutes)),
                stoppingToken);
        }
    }
}
