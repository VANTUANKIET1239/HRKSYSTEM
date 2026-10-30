using Microsoft.EntityFrameworkCore;
using Core.Persistence.Repositories;

namespace Core.Scheduling;

public sealed class UnitOfWorkSchedulingStore(IUnitOfWork unitOfWork) : ISchedulingStore
{
    public Task<List<JobDefinition>> GetDefinitionsAsync(CancellationToken ct) =>
        unitOfWork.ReadOnlyRepository<JobDefinition>().Query().OrderBy(x => x.Id).ToListAsync(ct);

    public Task<JobDefinition?> GetDefinitionAsync(long id, CancellationToken ct) =>
        unitOfWork.ReadOnlyRepository<JobDefinition>().Query().SingleOrDefaultAsync(x => x.Id == id, ct);

    public Task StartAsync(JobExecution execution, CancellationToken ct) => unitOfWork.ExecuteInTransactionAsync(async () =>
    {
        // A new firing of the same non-concurrent JobKey means an earlier RUNNING
        // execution was interrupted. Business handlers resume from durable checkpoints.
        var interrupted = await unitOfWork.Repository<JobExecution>().Query()
            .Where(x => x.JobDefinitionId == execution.JobDefinitionId && x.Status == "RUNNING").ToListAsync(ct);
        foreach (var previous in interrupted)
        {
            previous.Status = "INTERRUPTED";
            previous.CompletedAtUtc = DateTime.UtcNow;
        }
        await unitOfWork.Repository<JobExecution>().AddAsync(execution);
    }, ct);

    public Task FinishAsync(Guid id, string status, int attempts, string? resultJson, string? error, CancellationToken ct) =>
        unitOfWork.ExecuteInTransactionAsync(async () =>
    {
        var execution = await unitOfWork.Repository<JobExecution>().Query().SingleAsync(x => x.Id == id, ct);
        execution.Status = status;
        execution.Attempts = attempts;
        execution.ResultJson = resultJson;
        execution.Error = error is null ? null : error[..Math.Min(error.Length, 2000)];
        execution.CompletedAtUtc = DateTime.UtcNow;
    }, ct);
}
