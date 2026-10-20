using System.Text.Json;
using Core.TransactionalMessaging.Entities;
using Core.TransactionalMessaging.Outbox;
using GAME.Application.Events;
using GAME.Application.Interfaces;
using GAME.Application.Services;
using GAME.Domain.Entities;
using GAME.Infrastructure.Messaging;
using GAME.Infrastructure.Scheduling;
using Xunit;

namespace GAME.Domain.Tests;

public class EventResetTests
{
    [Fact]
    public async Task Settlement_ClosesMissedDaysOnce_PreservesCurrentAndOtherPlayers_UsesFrozenRewards()
    {
        var store = new TowerTestStore { HasActiveTransaction = true };
        var activities = new ActivityCapture();
        var outbox = new OutboxCapture();
        var settlement = new TowerEventService(store, activities, outbox, new EventResetSchedule(store));
        var cutoff = new DateTime(2026, 10, 20, 5, 0, 0, DateTimeKind.Utc);
        var gameEvent = new HrkGameEvent { Id = 1 };
        store.Data<HrkPlayer>().Add(new() { Id = 1, UserId = "player-a" });
        var first = Progress(1, 1, cutoff.AddDays(-1));
        var second = Progress(2, 1, cutoff);
        var current = Progress(3, 1, cutoff.AddDays(1));
        var other = Progress(4, 2, cutoff);
        store.Data<HrkPlayerEventPeriodProgress>().AddRange([first, second, current, other]);
        store.Data<HrkTowerChestConfig>().Add(new() { Id = 7, EventId = 1, FloorNumber = 10, IsActive = true, RewardsJson = "[{\"Quantity\":999}]" });
        first.EventPeriod.ChestSnapshotJson = JsonSerializer.Serialize(new[] { new EventChestSnapshot(7, 10, "[{\"Quantity\":5}]") });
        second.EventPeriod.ChestSnapshotJson = first.EventPeriod.ChestSnapshotJson;
        store.Data<HrkPlayerEventRewardClaim>().Add(new() { PlayerId = 1, EventPeriodId = first.EventPeriodId, ClaimType = "MILESTONE_CHEST", TargetId = 7 });
        store.Data<HrkPlayerTowerRun>().Add(new() { PlayerId = 1, EventPeriodId = first.EventPeriodId, Status = "IN_PROGRESS" });
        store.Data<HrkTowerQuickClimbJob>().Add(new() { PlayerId = 1, EventPeriodId = second.EventPeriodId, Status = "PROCESSING", Version = 3, JobId = "quick-1" });
        var executionId = Guid.NewGuid();

        Assert.Equal(2, await settlement.CloseExpiredPeriodsAsync(1, gameEvent, cutoff, executionId, default));
        Assert.Equal("[{\"Quantity\":5}]", Assert.Single(store.Data<HrkPlayerPendingReward>()).RewardItemsJson);
        Assert.NotNull(first.ClosedAtUtc);
        Assert.Equal(executionId, second.ResetExecutionId);
        Assert.Null(current.ClosedAtUtc);
        Assert.Null(other.ClosedAtUtc);
        Assert.Equal("EXPIRED", Assert.Single(store.Data<HrkPlayerTowerRun>()).Status);
        Assert.Equal(first.EventPeriod.EndAtUtc, store.Data<HrkPlayerTowerRun>()[0].EndAtUtc);
        Assert.Equal("EXPIRED", Assert.Single(store.Data<HrkTowerQuickClimbJob>()).Status);
        Assert.Equal(4, store.Data<HrkTowerQuickClimbJob>()[0].Version);
        Assert.Single(outbox.Messages);
        Assert.Equal(2, activities.Events.OfType<GameEventActivityEvent>().Count());
        var closed = activities.Events.OfType<GameEventActivityEvent>().Last();
        Assert.Equal(second.ClosedAtUtc, closed.OccurredAt!.Value.UtcDateTime);
        Assert.Equal(second.PeriodKey, closed.PeriodKey);

        Assert.Equal(0, await settlement.CloseExpiredPeriodsAsync(1, gameEvent, cutoff, Guid.NewGuid(), default));
        Assert.Single(store.Data<HrkPlayerPendingReward>());
        Assert.Equal(3, activities.Events.Count);
    }

    [Fact]
    public void ActivityMapper_PreservesPeriodAndJobIdentity_AcrossRetry()
    {
        var mapper = new PlayerActivityEventMapper();
        var execution = Guid.NewGuid();
        var source = new GameEventActivityEvent(42, "user", "PlayerEventPeriodClosed", "period:17", new { HighestFloor = 20 })
        {
            GameEventId = 3, EventPeriodId = 17, PeriodKey = "2026-10-19", JobExecutionId = execution,
            OccurredAt = new DateTimeOffset(2026, 10, 20, 5, 0, 0, TimeSpan.Zero)
        };
        var first = mapper.Map(source).Message;
        var retry = mapper.Map(source with { JobExecutionId = Guid.NewGuid() }).Message;
        Assert.Equal(first.EventId, retry.EventId);
        Assert.Equal(3, first.GameEventId);
        Assert.Equal(17, first.EventPeriodId);
        Assert.Equal(execution, first.JobExecutionId);
        Assert.Equal(source.OccurredAt, first.OccurredAt);
    }

    [Fact]
    public async Task RequestTimeProgress_ClosesPreviousPeriodAndPreservesAllTimeRecord()
    {
        var store = new TowerTestStore { HasActiveTransaction = true };
        var activities = new ActivityCapture();
        var service = new TowerEventService(store, activities, new OutboxCapture(), new EventResetSchedule(store));
        var gameEvent = new HrkGameEvent { Id = 1, InitialLives = 3 };
        var old = Progress(1, 1, DateTime.UtcNow.Date.AddDays(-1));
        old.EventPeriod.ChestSnapshotJson = "[]";
        var currentPeriod = new HrkEventPeriod
        {
            Id = 2, EventId = 1, PeriodKey = "current", StartAtUtc = DateTime.UtcNow.Date, EndAtUtc = DateTime.UtcNow.Date.AddDays(1)
        };
        store.Data<HrkPlayer>().Add(new() { Id = 1, UserId = "player-a" });
        store.Data<HrkPlayerEventPeriodProgress>().Add(old);
        store.Data<HrkPlayerEventAllTimeRecord>().Add(new() { PlayerId = 1, EventId = 1, HighestFloorAllTime = 42 });

        var current = await service.GetOrCreatePlayerPeriodProgressAsync(1, gameEvent, currentPeriod);

        Assert.NotNull(old.ClosedAtUtc);
        Assert.Equal(20, old.HighestFloorInPeriod);
        Assert.Equal(2, current.EventPeriodId);
        Assert.Equal(1, current.CurrentFloor);
        Assert.Equal(3, current.RemainingLives);
        Assert.Equal(0, current.QuickClimbRunsUsed);
        Assert.Equal(42, Assert.Single(store.Data<HrkPlayerEventAllTimeRecord>()).HighestFloorAllTime);
        Assert.Equal("PlayerEventPeriodClosed", Assert.Single(activities.Events.OfType<GameEventActivityEvent>()).ActivityType);
    }

    [Fact]
    public async Task RequestTimePeriod_UsesTheSameManagedScheduleAsQuartz()
    {
        var service = new TowerEventService(new TowerTestStore(), new ActivityCapture(), new OutboxCapture(), new MidnightSchedule());
        var period = await service.ReadCurrentPeriodAsync(new() { Id = 1, TimeZoneId = "Asia/Ho_Chi_Minh", ResetTime = TimeSpan.FromHours(12) });
        Assert.Equal(DateTime.UtcNow.Date, period.StartAtUtc);
        Assert.Equal(DateTime.UtcNow.Date.AddDays(1), period.EndAtUtc);
    }

    [Theory]
    [InlineData("0 0 0 * * ?", 0)]
    [InlineData("0 0 12 * * ?", 12)]
    public void DailySchedule_MapsManagedCronToPeriodBoundary(string cron, int hour)
    {
        var time = DailyEventSchedule.Parse(cron);
        Assert.Equal(TimeSpan.FromHours(hour), time);
        Assert.Equal(cron, DailyEventSchedule.ToCron(time));
        Assert.Throws<ArgumentException>(() => DailyEventSchedule.Parse("0 0/5 * * * ?"));
    }

    private static HrkPlayerEventPeriodProgress Progress(long id, long playerId, DateTime end) => new()
    {
        Id = id, PlayerId = playerId, EventId = 1, EventPeriodId = id, HighestFloorInPeriod = 20,
        PeriodKey = end.AddDays(-1).ToString("yyyy-MM-dd"), EventPeriod = new()
        { Id = id, EventId = 1, PeriodKey = end.AddDays(-1).ToString("yyyy-MM-dd"), StartAtUtc = end.AddDays(-1), EndAtUtc = end }
    };

    private sealed class MidnightSchedule : IEventResetSchedule
    {
        public Task<(string TimeZoneId, TimeSpan ResetTime)> GetAsync(HrkGameEvent gameEvent, CancellationToken ct) => Task.FromResult(("UTC", TimeSpan.Zero));
    }
    private sealed class ActivityCapture : IPlayerActivityEvents
    {
        public List<PlayerActivityEvent> Events { get; } = [];
        public void Raise(PlayerActivityEvent activity) => Events.Add(activity);
    }
    private sealed class OutboxCapture : IOutboxWriter
    {
        public List<object> Messages { get; } = [];
        public OutboxMessage Add<T>(T message, string eventName, string publisherName, string routingKey,
            int eventVersion = 1, string? partitionKey = null, long? sequence = null)
        { Messages.Add(message!); return new(); }
    }
}
