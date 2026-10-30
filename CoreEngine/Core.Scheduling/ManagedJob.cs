using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Core.Scheduling;

[DisallowConcurrentExecution]
public sealed class ManagedJob(JobExecutionRunner runner) : IJob
{
    public Task Execute(IJobExecutionContext context) => runner.RunAsync(
        long.Parse(context.MergedJobDataMap.GetString("DefinitionId")!, System.Globalization.CultureInfo.InvariantCulture),
        (context.ScheduledFireTimeUtc ?? context.FireTimeUtc).UtcDateTime,
        context.Scheduler.SchedulerInstanceId, context.CancellationToken);
}
