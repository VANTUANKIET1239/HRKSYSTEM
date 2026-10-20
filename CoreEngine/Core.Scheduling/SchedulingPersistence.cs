using Microsoft.EntityFrameworkCore;
using Core.Persistence.Repositories;

namespace Core.Scheduling;

public static class SchedulingPersistence
{
    public static void ConfigureScheduling(this ModelBuilder model, string prefix = "HRK_")
    {
        model.Entity<JobDefinition>(b =>
        {
            b.ToTable(prefix + "JobDefinitions");
            b.HasKey(x => x.Id);
            b.HasIndex(x => x.JobCode).IsUnique();
            b.HasIndex(x => new { x.HandlerKey, x.ScopeKey }).IsUnique();
            b.Property(x => x.JobCode).HasMaxLength(150);
            b.Property(x => x.Name).HasMaxLength(200);
            b.Property(x => x.Description).HasMaxLength(1000);
            b.Property(x => x.HandlerKey).HasMaxLength(100);
            b.Property(x => x.ScopeKey).HasMaxLength(100);
            b.Property(x => x.CronExpression).HasMaxLength(150);
            b.Property(x => x.TimeZoneId).HasMaxLength(100);
            b.Property(x => x.MisfirePolicy).HasMaxLength(30);
            b.Property(x => x.ParametersJson).HasColumnType("nvarchar(max)");
            b.Property(x => x.UpdatedBy).HasMaxLength(128);
            b.Property(x => x.ConfigVersion).IsConcurrencyToken();
        });
        model.Entity<JobExecution>(b =>
        {
            b.ToTable(prefix + "JobExecutions");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.JobDefinitionId, x.StartedAtUtc });
            b.Property(x => x.Status).HasMaxLength(30);
            b.Property(x => x.Error).HasMaxLength(2000);
            b.Property(x => x.InstanceId).HasMaxLength(200);
            b.HasOne<JobDefinition>().WithMany().HasForeignKey(x => x.JobDefinitionId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}

public interface ISchedulingStore
{
    Task<List<JobDefinition>> GetDefinitionsAsync(CancellationToken ct);
    Task<JobDefinition?> GetDefinitionAsync(long id, CancellationToken ct);
    Task StartAsync(JobExecution execution, CancellationToken ct);
    Task FinishAsync(Guid id, string status, int attempts, string? resultJson, string? error, CancellationToken ct);
}

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
