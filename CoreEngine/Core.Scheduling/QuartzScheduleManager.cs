using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Core.Scheduling;

public sealed class QuartzScheduleManager(ISchedulerFactory factory, IServiceScopeFactory scopes,
    SchedulingOptions options, ILogger<QuartzScheduleManager> logger) : IJobScheduleManager
{
    private readonly SemaphoreSlim _gate = new(1);
    private readonly HashSet<long> _started = [];

    public Task SynchronizeAsync(CancellationToken ct = default) => SynchronizeCoreAsync(null, ct);

    private async Task SynchronizeCoreAsync(long? manualDefinitionId, CancellationToken ct)
    {
        await _gate.WaitAsync(ct);
        try
        {
            await using var scope = scopes.CreateAsyncScope();
            var definitions = await scope.ServiceProvider.GetRequiredService<ISchedulingStore>().GetDefinitionsAsync(ct);
            var scheduler = await factory.GetScheduler(ct);
            var knownHandlers = scope.ServiceProvider.GetServices<IScheduledJobHandler>().Select(x => x.HandlerKey).ToHashSet(StringComparer.Ordinal);
            foreach (var definition in definitions)
            {
                try { await SynchronizeDefinitionAsync(definition); }
                catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
                catch (Exception exception)
                {
                    logger.LogError(exception, "Cannot synchronize JobCode={JobCode}; other definitions will continue.", definition.JobCode);
                }
            }

            async Task SynchronizeDefinitionAsync(JobDefinition definition)
            {
                var key = Key(definition.Id);
                if (!definition.IsEnabled)
                {
                    await scheduler.DeleteJob(key, ct);
                    _started.Remove(definition.Id);
                    return;
                }
                JobDefinitionValidator.Validate(definition);
                if (!knownHandlers.Contains(definition.HandlerKey))
                    throw new InvalidOperationException($"Handler '{definition.HandlerKey}' is not registered.");
                var existing = await scheduler.GetJobDetail(key, ct);
                var version = definition.ConfigVersion.ToString(System.Globalization.CultureInfo.InvariantCulture);
                if (existing?.JobDataMap.GetString("ConfigVersion") != version)
                {
                    var job = JobBuilder.Create<ManagedJob>().WithIdentity(key).StoreDurably().RequestRecovery()
                        .UsingJobData("DefinitionId", definition.Id.ToString(System.Globalization.CultureInfo.InvariantCulture))
                        .UsingJobData("ConfigVersion", version).Build();
                    await scheduler.AddJob(job, true, ct);
                }
                var cronKey = new TriggerKey($"job-{definition.Id}-cron", options.Group);
                var cron = TriggerBuilder.Create().WithIdentity(cronKey).ForJob(key)
                    .WithCronSchedule(definition.CronExpression, c =>
                    {
                        c.InTimeZone(TimeZoneInfo.FindSystemTimeZoneById(definition.TimeZoneId));
                        if (definition.MisfirePolicy == "DoNothing") c.WithMisfireHandlingInstructionDoNothing();
                        else c.WithMisfireHandlingInstructionFireAndProceed();
                    }).Build();
                var currentCron = await scheduler.GetTrigger(cronKey, ct) as ICronTrigger;
                if (currentCron is null)
                {
                    try { await scheduler.ScheduleJob(cron, ct); }
                    catch (ObjectAlreadyExistsException) { /* Another cluster node installed this trigger. */ }
                }
                else if (currentCron.CronExpressionString != definition.CronExpression || currentCron.TimeZone.Id != definition.TimeZoneId ||
                    currentCron.MisfireInstruction != cron.MisfireInstruction)
                    await scheduler.RescheduleJob(cronKey, cron, ct);
                var recoveryKey = new TriggerKey($"job-{definition.Id}-recovery", options.Group);
                var currentRecovery = await scheduler.GetTrigger(recoveryKey, ct) as ISimpleTrigger;
                if (definition.RecoveryIntervalSeconds == 0) await scheduler.UnscheduleJob(recoveryKey, ct);
                else if (currentRecovery?.RepeatInterval != TimeSpan.FromSeconds(definition.RecoveryIntervalSeconds))
                {
                    var recovery = TriggerBuilder.Create().WithIdentity(recoveryKey).ForJob(key)
                        .StartAt(DateTimeOffset.UtcNow.AddSeconds(definition.RecoveryIntervalSeconds))
                        .WithSimpleSchedule(s => s.WithIntervalInSeconds(definition.RecoveryIntervalSeconds).RepeatForever()
                            .WithMisfireHandlingInstructionNextWithRemainingCount()).Build();
                    if (currentRecovery is null)
                    {
                        try { await scheduler.ScheduleJob(recovery, ct); }
                        catch (ObjectAlreadyExistsException) { /* Another cluster node installed this trigger. */ }
                    }
                    else await scheduler.RescheduleJob(recoveryKey, recovery, ct);
                }
                // Startup/re-enable reconciliation also processes overdue business periods.
                if (!_started.Contains(definition.Id))
                {
                    if (manualDefinitionId != definition.Id) await scheduler.TriggerJob(key, ct);
                    _started.Add(definition.Id);
                }
            }
        }
        finally { _gate.Release(); }
    }

    public async Task TriggerAsync(long definitionId, CancellationToken ct = default)
    {
        await using var scope = scopes.CreateAsyncScope();
        var definition = await scope.ServiceProvider.GetRequiredService<ISchedulingStore>().GetDefinitionAsync(definitionId, ct);
        if (definition is null || !definition.IsEnabled) throw new InvalidOperationException("Job is missing or disabled.");
        await SynchronizeCoreAsync(definitionId, ct);
        await (await factory.GetScheduler(ct)).TriggerJob(Key(definitionId), ct);
    }

    private JobKey Key(long id) => new($"job-{id}", options.Group);
}
