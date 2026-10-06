using Core.TransactionalMessaging.Outbox;
using GAME.Application.Events;
using GAME.Infrastructure.Data;
using GAME.Infrastructure.Messaging;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GAME.Domain.Tests;

public class PlayerActivityEventTests
{
    [Fact]
    public void Mapping_HasStableIdentityAndSeparatesStartFromEnd()
    {
        var mapper = new PlayerActivityEventMapper();
        var started = new QuickClimbStartedEvent(1, "user", "job", 1, 1, 10, 0, 1);
        var first = mapper.Map(started);
        Assert.Equal(first.Message.EventId, mapper.Map(started).Message.EventId);
        var ended = mapper.Map(new QuickClimbEndedEvent(1, "user", "job", "STOPPED_DEFEAT", 1, 3, 10, 2, "FIRST_DEFEAT", 4));
        Assert.NotEqual(first.Message.EventId, ended.Message.EventId);
        Assert.Equal("QuickClimbCompleted", ended.Message.ActivityType);
        Assert.Equal("STOPPED_DEFEAT", ended.Message.Payload.GetProperty("Status").GetString());
        Assert.Equal("job", ended.PartitionKey);
        Assert.Equal(4, ended.Sequence);
        Assert.Throws<ArgumentException>(() => mapper.Map(new QuickClimbEndedEvent(1, "user", "job", "PROCESSING", 1, 3, 10, 2, null, 4)));
    }

    [Fact]
    public void RaisingEvent_StagesOutboxInTheOwningDbContextWithoutSaving()
    {
        using var db = new GameDbContext(new DbContextOptionsBuilder<GameDbContext>()
            .UseSqlServer("Server=unused;Database=unused;Integrated Security=True").Options);
        var events = new PlayerActivityEvents(new EfOutboxWriter<GameDbContext>(db), new(), db);
        var activity = new ItemEnhancedEvent(1, "user", Guid.NewGuid(), 7, 2, 1, 2, true, 100);
        events.Raise(activity);
        events.Raise(activity);
        var entry = Assert.Single(db.ChangeTracker.Entries<Core.TransactionalMessaging.Entities.OutboxMessage>());
        Assert.Equal(EntityState.Added, entry.State);
        Assert.Equal("GameEvents", entry.Entity.PublisherName);
        Assert.Equal("game.activity.recorded.v1", entry.Entity.RoutingKey);
    }
}
