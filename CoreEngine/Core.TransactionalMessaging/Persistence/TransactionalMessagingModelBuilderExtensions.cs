using Core.TransactionalMessaging.Entities;
using Microsoft.EntityFrameworkCore;

namespace Core.TransactionalMessaging.Persistence;

public static class TransactionalMessagingModelBuilderExtensions
{
    public static ModelBuilder ConfigureTransactionalMessaging(
        this ModelBuilder modelBuilder,
        string outboxTable,
        string inboxTable,
        string schema = "dbo")
    {
        modelBuilder.Entity<OutboxMessage>(entity =>
        {
            entity.ToTable(outboxTable, schema);
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.Status, x.NextAttemptAt, x.CreatedAt });
            entity.HasIndex(x => x.LockedUntil);
            entity.HasIndex(x => new { x.PartitionKey, x.Sequence });
            entity.Property(x => x.EventName).HasMaxLength(200).IsRequired();
            entity.Property(x => x.PublisherName).HasMaxLength(100).IsRequired();
            entity.Property(x => x.RoutingKey).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Payload).HasColumnType("nvarchar(max)").IsRequired();
            entity.Property(x => x.LockToken).HasMaxLength(64);
            entity.Property(x => x.LastError).HasMaxLength(2000);
            entity.Property(x => x.PartitionKey).HasMaxLength(200);
            entity.Property(x => x.CorrelationId).HasMaxLength(128);
            entity.Property(x => x.TraceParent).HasMaxLength(128);
            entity.Property(x => x.TraceState).HasMaxLength(512);
        });

        modelBuilder.Entity<InboxMessage>(entity =>
        {
            entity.ToTable(inboxTable, schema);
            entity.HasKey(x => new { x.ConsumerName, x.MessageId });
            entity.HasIndex(x => x.ProcessedAt);
            entity.Property(x => x.ConsumerName).HasMaxLength(150);
            entity.Property(x => x.MessageId).HasMaxLength(100);
        });

        return modelBuilder;
    }
}
