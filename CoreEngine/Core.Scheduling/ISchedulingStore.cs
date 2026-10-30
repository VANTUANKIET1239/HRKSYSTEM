using Microsoft.EntityFrameworkCore;
using Core.Persistence.Repositories;

namespace Core.Scheduling;

public interface ISchedulingStore
{
    Task<List<JobDefinition>> GetDefinitionsAsync(CancellationToken ct);
    Task<JobDefinition?> GetDefinitionAsync(long id, CancellationToken ct);
    Task StartAsync(JobExecution execution, CancellationToken ct);
    Task FinishAsync(Guid id, string status, int attempts, string? resultJson, string? error, CancellationToken ct);
}
