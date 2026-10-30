using System.Collections.Concurrent;
using System.Text.Json;
using Core.Scheduling;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Quartz;
using Xunit;

namespace Core.Modules.Tests;

public partial class ManagedSchedulingTests
{
    [Fact]
    public async Task Runner_RetriesInFreshScopes_AndKeepsConfigurationSnapshot()
    {
        var state = new ProbeState { FailFirst = true };
        var store = new MemoryStore();
        var services = new ServiceCollection().AddLogging();
        services.AddSingleton(state);
        services.AddSingleton<ISchedulingStore>(store);
        services.AddScoped<IScheduledJobHandler, ProbeHandler>();
        await using var provider = services.BuildServiceProvider();
        var runner = new JobExecutionRunner(provider.GetRequiredService<IServiceScopeFactory>(), NullLogger<JobExecutionRunner>.Instance);
        await runner.RunAsync(1, DateTime.UtcNow, "test-instance", default);

        var execution = Assert.Single(store.Executions.Values);
        Assert.Equal("SUCCEEDED", execution.Status);
        Assert.Equal(2, execution.Attempts);
        Assert.Equal(2, state.Scopes.Distinct().Count());
        Assert.Equal(7, execution.ConfigVersion);
        Assert.Equal("{}", JsonSerializer.Deserialize<JobDefinition>(execution.DefinitionSnapshotJson)!.ParametersJson);
        Assert.NotNull(execution.CompletedAtUtc);
    }

    [Fact]
    public async Task Runner_RecordsCooperativeTimeout_AndSkipsDisabledJob()
    {
        var state = new ProbeState { WaitForCancellation = true };
        var store = new MemoryStore();
        store.Definition.TimeoutSeconds = 1;
        store.Definition.MaxRetryAttempts = 0;
        var services = new ServiceCollection().AddLogging();
        services.AddSingleton(state);
        services.AddSingleton<ISchedulingStore>(store);
        services.AddScoped<IScheduledJobHandler, ProbeHandler>();
        await using var provider = services.BuildServiceProvider();
        var runner = new JobExecutionRunner(provider.GetRequiredService<IServiceScopeFactory>(), NullLogger<JobExecutionRunner>.Instance);
        await runner.RunAsync(1, DateTime.UtcNow, "test", default);
        Assert.Equal("TIMED_OUT", Assert.Single(store.Executions.Values).Status);
        store.Definition.IsEnabled = false;
        await runner.RunAsync(1, DateTime.UtcNow, "test", default);
        Assert.Single(store.Executions);
    }

    [Fact]
    public async Task Quartz_ReconcilesTimeZoneAndRecoverySchedule_ExecutesAndDisablesJob()
    {
        var state = new ProbeState();
        var store = new MemoryStore();
        store.Definition.CronExpression = "0 0 12 * * ?";
        store.Definition.TimeZoneId = "Asia/Ho_Chi_Minh";
        store.Definition.RecoveryIntervalSeconds = 60;
        var services = new ServiceCollection().AddLogging();
        services.AddManagedScheduling(o =>
        {
            o.Enabled = true;
            o.UsePersistentStore = false;
            o.SchedulerName = "test-" + Guid.NewGuid().ToString("N");
        });
        services.AddSingleton(state);
        services.AddSingleton<ISchedulingStore>(store);
        services.AddScoped<IScheduledJobHandler, ProbeHandler>();
        await using var provider = services.BuildServiceProvider();
        var scheduler = await provider.GetRequiredService<ISchedulerFactory>().GetScheduler();
        try
        {
            await scheduler.Start();
            var manager = provider.GetRequiredService<IJobScheduleManager>();
            await manager.SynchronizeAsync();
            await state.Completed.Task.WaitAsync(TimeSpan.FromSeconds(10));
            var cron = Assert.IsAssignableFrom<ICronTrigger>(await scheduler.GetTrigger(new("job-1-cron", "managed")));
            Assert.Equal("Asia/Ho_Chi_Minh", cron.TimeZone.Id);
            Assert.Equal("0 0 12 * * ?", cron.CronExpressionString);
            // Probe after trigger creation; a fixed historical date is clipped by StartTimeUtc.
            var probeDay = new DateTimeOffset(DateTime.UtcNow.Date.AddDays(2), TimeSpan.Zero);
            var next = cron.GetFireTimeAfter(probeDay.AddHours(5).AddSeconds(-1));
            Assert.Equal(probeDay.AddHours(5), next);

            store.Definition.CronExpression = "0 0 0 * * ?";
            store.Definition.RecoveryIntervalSeconds = 90;
            store.Definition.ConfigVersion++;
            await manager.SynchronizeAsync();
            Assert.Equal("0 0 0 * * ?", ((ICronTrigger)(await scheduler.GetTrigger(new("job-1-cron", "managed")))!).CronExpressionString);
            Assert.Equal(TimeSpan.FromSeconds(90), ((ISimpleTrigger)(await scheduler.GetTrigger(new("job-1-recovery", "managed")))!).RepeatInterval);
            store.Definition.IsEnabled = false;
            await manager.SynchronizeAsync();
            Assert.False(await scheduler.CheckExists(new JobKey("job-1", "managed")));
        }
        finally { await scheduler.Shutdown(true); }
    }
}
