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
