using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Core.Scheduling;

internal sealed class ScheduleSynchronizationWorker(IJobScheduleManager manager, SchedulingOptions options,
    ILogger<ScheduleSynchronizationWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try { await manager.SynchronizeAsync(stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex) { logger.LogError(ex, "Managed schedule synchronization failed; it will be retried."); }
            try { await Task.Delay(TimeSpan.FromSeconds(options.SynchronizationIntervalSeconds), stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
        }
    }
}
