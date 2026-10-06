using Activity.Domain;
using Core.TransactionalMessaging.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Activity.Infrastructure;

public sealed class ActivityDbContext(DbContextOptions<ActivityDbContext> options) : DbContext(options)
{
    public DbSet<PlayerActivity> PlayerActivities => Set<PlayerActivity>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.ConfigureTransactionalMessaging("Activity_OutboxMessages", "Activity_ProcessedMessages");
        model.Entity<PlayerActivity>(entity =>
        {
            entity.ToTable("PlayerActivities");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.ActivityId).IsUnique();
            entity.HasIndex(x => new { x.PlayerId, x.OccurredAt }).IsDescending(false, true);
            entity.HasIndex(x => new { x.ActivityType, x.OccurredAt }).IsDescending(false, true);
            entity.HasIndex(x => x.CorrelationId);
            entity.HasIndex(x => new { x.EntityType, x.EntityId });
            entity.Property(x => x.ActivityType).HasMaxLength(100);
            entity.Property(x => x.UserId).HasMaxLength(128);
            entity.Property(x => x.EntityType).HasMaxLength(100);
            entity.Property(x => x.EntityId).HasMaxLength(200);
            entity.Property(x => x.SourceService).HasMaxLength(100);
            entity.Property(x => x.CorrelationId).HasMaxLength(128);
            entity.Property(x => x.TraceId).HasMaxLength(32);
            entity.Property(x => x.PayloadJson).HasColumnType("nvarchar(max)");
        });
    }
}
