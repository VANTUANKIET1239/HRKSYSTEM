using System.Collections.Concurrent;
using System.Text.Json;
using Core.Scheduling;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Quartz;
using Xunit;

namespace Core.Modules.Tests;

public class ManagedSchedulingTests
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

    private sealed class ProbeState
    {
        public bool FailFirst;
        public bool WaitForCancellation;
        public int Calls;
        public ConcurrentQueue<Guid> Scopes { get; } = new();
        public TaskCompletionSource Completed { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    private sealed class ProbeHandler(ProbeState state) : IScheduledJobHandler
    {
        private readonly Guid _scope = Guid.NewGuid();
        public string HandlerKey => "PROBE";
        public async Task<ScheduledJobResult> ExecuteAsync(ScheduledJobContext context, CancellationToken ct)
        {
            state.Scopes.Enqueue(_scope);
            int call = Interlocked.Increment(ref state.Calls);
            if (state.FailFirst && call == 1) throw new InvalidOperationException("Retry probe");
            if (state.WaitForCancellation) await Task.Delay(Timeout.Infinite, ct);
            state.Completed.TrySetResult();
            return new(1);
        }
    }

    private sealed class MemoryStore : ISchedulingStore
    {
        public JobDefinition Definition { get; } = new()
        {
            Id = 1, JobCode = "probe", HandlerKey = "PROBE", CronExpression = "0 0 0 1 1 ? 2099",
            ConfigVersion = 7, RetryDelaySeconds = 0, RecoveryIntervalSeconds = 0
        };
        public ConcurrentDictionary<Guid, JobExecution> Executions { get; } = new();
        public Task<List<JobDefinition>> GetDefinitionsAsync(CancellationToken ct) => Task.FromResult(new List<JobDefinition> { Definition });
        public Task<JobDefinition?> GetDefinitionAsync(long id, CancellationToken ct) => Task.FromResult(
            id == Definition.Id ? JsonSerializer.Deserialize<JobDefinition>(JsonSerializer.Serialize(Definition)) : null);
        public Task StartAsync(JobExecution execution, CancellationToken ct) { Executions[execution.Id] = execution; return Task.CompletedTask; }
        public Task FinishAsync(Guid id, string status, int attempts, string? resultJson, string? error, CancellationToken ct)
        {
            var value = Executions[id];
            value.Status = status; value.Attempts = attempts; value.ResultJson = resultJson;
            value.Error = error; value.CompletedAtUtc = DateTime.UtcNow;
            return Task.CompletedTask;
        }
    }
}
