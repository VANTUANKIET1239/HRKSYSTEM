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

public sealed class JobExecutionRunner(IServiceScopeFactory scopes, ILogger<JobExecutionRunner> logger)
{
    public async Task RunAsync(long definitionId, DateTime scheduledAtUtc, string instanceId, CancellationToken ct)
    {
        JobDefinition? definition;
        var executionId = Guid.NewGuid();
        await using (var scope = scopes.CreateAsyncScope())
        {
            var store = scope.ServiceProvider.GetRequiredService<ISchedulingStore>();
            definition = await store.GetDefinitionAsync(definitionId, ct);
            if (definition is null || !definition.IsEnabled) return;
            JobDefinitionValidator.Validate(definition);
            await store.StartAsync(new JobExecution
            {
                Id = executionId, JobDefinitionId = definition.Id, ConfigVersion = definition.ConfigVersion,
                DefinitionSnapshotJson = JsonSerializer.Serialize(definition), ScheduledAtUtc = scheduledAtUtc,
                StartedAtUtc = DateTime.UtcNow, InstanceId = instanceId
            }, ct);
        }

        string status = "FAILED";
        string? error = null;
        string? resultJson = null;
        int attempts = 0;
        try
        {
            for (int attempt = 0; attempt <= definition.MaxRetryAttempts; attempt++)
            {
                ct.ThrowIfCancellationRequested();
                attempts++;
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
                timeout.CancelAfter(TimeSpan.FromSeconds(definition.TimeoutSeconds));
                try
                {
                    // Every attempt has a fresh service/DbContext scope. Execution-history
                    // writes use separate scopes and cannot commit gameplay changes.
                    await using var scope = scopes.CreateAsyncScope();
                    var handler = scope.ServiceProvider.GetServices<IScheduledJobHandler>()
                        .Single(x => x.HandlerKey == definition.HandlerKey);
                    var result = await handler.ExecuteAsync(new(executionId, definition, scheduledAtUtc), timeout.Token);
                    timeout.Token.ThrowIfCancellationRequested();
                    resultJson = JsonSerializer.Serialize(result);
                    status = "SUCCEEDED";
                    error = null;
                    break;
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
                catch (Exception exception)
                {
                    status = timeout.IsCancellationRequested ? "TIMED_OUT" : "FAILED";
                    error = exception.GetType().Name + ": " + exception.Message;
                    logger.LogWarning("ScheduledJobAttemptFailed: JobCode={JobCode}, ExecutionId={ExecutionId}, Attempt={Attempt}, ErrorType={ErrorType}",
                        definition.JobCode, executionId, attempts, exception.GetType().Name);
                    if (attempt < definition.MaxRetryAttempts)
                        await Task.Delay(TimeSpan.FromSeconds(definition.RetryDelaySeconds), ct);
                }
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { status = "CANCELLED"; }
        finally
        {
            using var completionTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            await using var scope = scopes.CreateAsyncScope();
            await scope.ServiceProvider.GetRequiredService<ISchedulingStore>()
                .FinishAsync(executionId, status, attempts, resultJson, error, completionTimeout.Token);
        }
        logger.LogInformation("ScheduledJobEnded: JobCode={JobCode}, ExecutionId={ExecutionId}, Status={Status}, Attempts={Attempts}",
            definition.JobCode, executionId, status, attempts);
    }
}

public static class JobDefinitionValidator
{
    public static void Validate(JobDefinition definition)
    {
        if (string.IsNullOrWhiteSpace(definition.JobCode) || definition.JobCode.Length > 150 ||
            string.IsNullOrWhiteSpace(definition.HandlerKey) || definition.HandlerKey.Length > 100 ||
            definition.ScopeKey.Length > 100 || definition.Name.Length > 200 || definition.Description.Length > 1000 ||
            definition.CronExpression.Length > 120 || !CronExpression.IsValidExpression(definition.CronExpression) ||
            definition.TimeZoneId.Length > 80 || definition.ConfigVersion < 1 ||
            definition.MaxRetryAttempts is < 0 or > 10 || definition.RetryDelaySeconds is < 0 or > 3600 ||
            definition.TimeoutSeconds is < 1 or > 86400 ||
            (definition.RecoveryIntervalSeconds != 0 && definition.RecoveryIntervalSeconds is < 30 or > 86400) ||
            definition.MisfirePolicy is not ("FireOnceNow" or "DoNothing"))
            throw new ArgumentException("Invalid scheduled job configuration.");
        _ = TimeZoneInfo.FindSystemTimeZoneById(definition.TimeZoneId);
        using var json = JsonDocument.Parse(definition.ParametersJson);
        if (json.RootElement.ValueKind != JsonValueKind.Object || definition.ParametersJson.Length > 16384)
            throw new ArgumentException("Job parameters must be a JSON object up to 16 KB.");
    }
}
