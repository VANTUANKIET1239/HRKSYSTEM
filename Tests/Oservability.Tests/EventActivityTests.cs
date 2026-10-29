using System.Text.Json;
using Activity.Application;
using Activity.Infrastructure;
using Core.Messaging.Contracts;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Oservability.Tests;

public class EventActivityTests
{
    [Fact]
    public void PlayerInitializationIsAcceptedWithPlayerOwnershipAndConfigVersion()
    {
        var activity = new GAME.Application.Events.PlayerInitializedEvent(42, "player", Guid.NewGuid(), 7, 2,
            new { InitialGold = 10000 });
        var message = new GAME.Infrastructure.Messaging.PlayerActivityEventMapper().Map(activity).Message;
        var mapped = ActivityMapping.Map(new(message, message.EventId.ToString("N"), "registration", null));
        Assert.Equal("PlayerInitialized", mapped.ActivityType);
        Assert.Equal("Player", mapped.EntityType);
        Assert.Equal("42", mapped.EntityId);
        Assert.Contains("ConfigVersion", mapped.PayloadJson);
        Assert.Null(mapped.GameEventId);
    }

    [Fact]
    public void CheckInActivityIsAcceptedWithDateAndCalendarMetadataWithoutTowerPeriod()
    {
        var activity = new GAME.Application.Events.CheckInClaimedEvent(42, "player", 10, "2026-10",
            new DateOnly(2026, 10, 21), Guid.NewGuid(), 1, new[] { new { Type = "GOLD", Quantity = 1000 } })
        { PeriodKey = "2026-10-21", OccurredAt = new DateTimeOffset(2026, 10, 21, 10, 0, 0, TimeSpan.FromHours(7)) };
        var message = new GAME.Infrastructure.Messaging.PlayerActivityEventMapper().Map(activity).Message;
        var mapped = ActivityMapping.Map(new(message, message.EventId.ToString("N"), "checkin-request", null));
        Assert.Equal("CheckInClaimed", mapped.ActivityType);
        Assert.Equal("2026-10-21", mapped.PeriodKey);
        Assert.Null(mapped.EventPeriodId);
        Assert.Null(mapped.GameEventId);
        Assert.Contains("CalendarId", mapped.PayloadJson);
    }

    [Fact]
    public void Mapping_PreservesQueryableEventDimensions_AndAcceptsOldMessages()
    {
        var id = Guid.NewGuid();
        var executionId = Guid.NewGuid();
        var message = new PlayerActivityRecordedV1(id, "PlayerEventPeriodClosed", 42, "player-a", "GameEvent", "3",
            "HRK.GAME", new DateTimeOffset(2026, 10, 20, 12, 0, 0, TimeSpan.FromHours(7)),
            JsonSerializer.SerializeToElement(new { HighestFloor = 20 }), 3, 17, "2026-10-19", executionId);
        var result = ActivityMapping.Map(new(message, id.ToString("N"), null, null));
        Assert.Equal(3, result.GameEventId);
        Assert.Equal(17, result.EventPeriodId);
        Assert.Equal("2026-10-19", result.PeriodKey);
        Assert.Equal(executionId, result.JobExecutionId);
        Assert.Equal(TimeSpan.Zero, result.OccurredAt.Offset);
        var old = JsonSerializer.Deserialize<PlayerActivityRecordedV1>(JsonSerializer.Serialize(new
        {
            message.EventId, message.ActivityType, message.PlayerId, message.UserId,
            message.EntityType, message.EntityId, message.SourceService, message.OccurredAt, message.Payload
        }))!;
        Assert.Null(ActivityMapping.Map(new(old, id.ToString("N"), null, null)).GameEventId);
        Assert.Throws<ArgumentException>(() => ActivityMapping.Map(new(message with { EventPeriodId = -1 }, id.ToString("N"), null, null)));
    }

    [Fact]
    public void Filters_UseAnExclusiveEndTime_AndEventColumnsAreIndexed()
    {
        var from = DateTimeOffset.UtcNow;
        Assert.False(new GetActivitiesQuery(FromUtc: from, ToUtc: from).IsValid);
        Assert.False(new GetActivitiesQuery(EventPeriodId: -1).IsValid);
        Assert.True(new GetActivitiesQuery(PlayerId: 42, GameEventId: 3, PeriodKey: "2026-10-19", FromUtc: from, ToUtc: from.AddDays(1)).IsValid);
        using var db = new ActivityDbContext(new DbContextOptionsBuilder<ActivityDbContext>()
            .UseSqlServer("Server=unused;Database=unused;Integrated Security=True").Options);
        var sql = db.Database.GenerateCreateScript();
        Assert.Contains("[GameEventId] int NULL", sql);
        Assert.Contains("[EventPeriodId] bigint NULL", sql);
        Assert.Contains("[PeriodKey] nvarchar(50) NULL", sql);
        Assert.Contains("IX_PlayerActivities_PlayerId_GameEventId_PeriodKey_OccurredAt", sql);
    }
}
