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
    private sealed class MemoryStore : ISchedulingStore
    {
        public JobDefinition Definition { get; } = new()
        {
            Id = 1,
            JobCode = "probe",
            HandlerKey = "PROBE",
            CronExpression = "0 0 0 1 1 ? 2099",
            ConfigVersion = 7,
            RetryDelaySeconds = 0,
            RecoveryIntervalSeconds = 0
        };
        public ConcurrentDictionary<Guid, JobExecution> Executions { get; } = new();

        public Task<List<JobDefinition>> GetDefinitionsAsync(CancellationToken ct) => Task.FromResult(new List<JobDefinition> { Definition });
        public Task<JobDefinition?> GetDefinitionAsync(long id, CancellationToken ct) => Task.FromResult(id == Definition.Id ? JsonSerializer.Deserialize<JobDefinition>(JsonSerializer.Serialize(Definition)) : null);
        public Task StartAsync(JobExecution execution, CancellationToken ct)
        {
            Executions[execution.Id] = execution;
            return Task.CompletedTask;
        }

        public Task FinishAsync(Guid id, string status, int attempts, string? resultJson, string? error, CancellationToken ct)
        {
            var value = Executions[id];
            value.Status = status;
            value.Attempts = attempts;
            value.ResultJson = resultJson;
            value.Error = error;
            value.CompletedAtUtc = DateTime.UtcNow;
            return Task.CompletedTask;
        }
    }
}
