using System.Text.Json;
using System.Globalization;
using GAME.Application.Common;
using GAME.Application.DTOs;
using GAME.Application.Events;
using GAME.Application.Features.Queries.CheckIn;
using GAME.Domain.Entities;
using GAME.Infrastructure.Messaging;
using Xunit;

namespace GAME.Domain.Tests;

public sealed class CheckInTests
{
    [Theory]
    [InlineData("2027-02", 28)]
    [InlineData("2028-02", 29)]
    [InlineData("2026-04", 30)]
    [InlineData("2026-10", 31)]
    public async Task CalendarShowsExactlyValidMonthDays_AndDoesNotWrite(string month, int count)
    {
        var store = new TowerTestStore();
        var bounds = CheckInCalendarPolicy.GetMonthBounds(month, "Asia/Ho_Chi_Minh", TimeSpan.Zero);
        var player = new HrkPlayer { Id = 1, UserId = "owner", Level = 1, IsActive = true };
        store.Data<HrkPlayer>().Add(player);
        var calendar = new HrkCheckInCalendar { Id = 1, MonthKey = month, IsPublished = true, StartAtUtc = bounds.StartUtc, EndAtUtc = bounds.EndUtc };
        store.Data<HrkCheckInCalendar>().Add(calendar);
        for (var day = 1; day <= count; day++) store.Data<HrkCheckInRewardConfig>().Add(new() { CalendarId = 1, DayOfMonth = day, RewardsJson = "[{\"Type\":\"GOLD\",\"Quantity\":1000}]" });
        var clock = new CheckInClock(bounds.StartUtc.AddDays(9).AddHours(1));
        var query = new GetCheckInCalendarQueryHandler(store, TestGamePlayers.Create(store), clock);
        var response = (await query.Handle(new("owner"), default)).Data!;
        Assert.Equal(count, response.Days.Count);
        Assert.Equal(10, Assert.Single(response.Days, x => x.Status == "CLAIMABLE").DayOfMonth);
        Assert.Equal(9, response.Days.Count(x => x.Status == "MISSED"));
        Assert.True(response.CanClaim);
        Assert.Equal(0, store.SaveCount);
        Assert.Empty(store.Data<HrkPlayerCheckInClaim>());
    }

    [Fact]
    public void DayAndMonthBoundaryUseGameResetTime_NotClientOrUtcDate()
    {
        var reset = TimeSpan.FromHours(4);
        var bounds = CheckInCalendarPolicy.GetMonthBounds("2026-10", "Asia/Ho_Chi_Minh", reset);
        var calendar = new HrkCheckInCalendar { MonthKey = "2026-10", ResetTime = reset, StartAtUtc = bounds.StartUtc, EndAtUtc = bounds.EndUtc };
        var before = CheckInCalendarPolicy.Resolve(calendar, new DateTime(2026, 10, 31, 20, 59, 59, DateTimeKind.Utc));
        Assert.Equal(new DateOnly(2026, 10, 31), before.GameDate); // November 1st, 03:59:59 in Vietnam.
        Assert.Equal(bounds.EndUtc, before.NextResetUtc);
        var next = CheckInCalendarPolicy.GetMonthBounds("2026-11", calendar.TimeZoneId, reset);
        Assert.Equal(bounds.EndUtc, next.StartUtc);
    }

    [Fact]
    public async Task NoPublishedCalendarIsClosedWithoutCreatingRows()
    {
        var store = new TowerTestStore();
        store.Data<HrkPlayer>().Add(new() { Id = 1, UserId = "owner", IsActive = true });
        var result = (await new GetCheckInCalendarQueryHandler(store, TestGamePlayers.Create(store), TimeProvider.System)
            .Handle(new("owner"), default)).Data!;
        Assert.False(result.IsOpen);
        Assert.False(result.CanClaim);
        Assert.Empty(result.Days);
        Assert.Equal(0, store.SaveCount);
    }

    [Fact]
    public async Task HistoryShowsGrantedSnapshot_AndAnotherPlayersClaimDoesNotBlockToday()
    {
        var store = new TowerTestStore();
        var bounds = CheckInCalendarPolicy.GetMonthBounds("2026-10", "UTC", TimeSpan.Zero);
        store.Data<HrkPlayer>().Add(new() { Id = 1, UserId = "owner", Level = 1, IsActive = true });
        store.Data<HrkCheckInCalendar>().Add(new() { Id = 1, MonthKey = "2026-10", TimeZoneId = "UTC", IsPublished = true, StartAtUtc = bounds.StartUtc, EndAtUtc = bounds.EndUtc });
        for (var day = 1; day <= 31; day++) store.Data<HrkCheckInRewardConfig>().Add(new() { CalendarId = 1, DayOfMonth = day, RewardsJson = "[{\"Type\":\"GOLD\",\"Quantity\":1000}]" });
        var receipt = new CheckInClaimResultDto(Guid.NewGuid(), 1, new(2026, 10, 1), bounds.StartUtc, 1, false, [new() { Type = "GOLD", Quantity = 500 }]);
        store.Data<HrkPlayerCheckInClaim>().Add(new() { PlayerId = 1, CalendarId = 1, CheckInDate = receipt.CheckInDate, ResultJson = JsonSerializer.Serialize(receipt) });
        store.Data<HrkPlayerCheckInClaim>().Add(new() { PlayerId = 2, CalendarId = 1, CheckInDate = new(2026, 10, 21), ResultJson = "{}" });
        var result = (await new GetCheckInCalendarQueryHandler(store, TestGamePlayers.Create(store), new CheckInClock(bounds.StartUtc.AddDays(20)))
            .Handle(new("owner"), default)).Data!;
        Assert.Equal(500, result.Days[0].Rewards.Single().Quantity);
        Assert.Equal("CLAIMED", result.Days[0].Status);
        Assert.Equal("CLAIMABLE", result.Days[20].Status);
        Assert.Equal(1, result.ClaimedDays);
    }

    [Fact]
    public void ActivityIdentityUsesPlayerCalendarAndDate_NotTransportRetryId()
    {
        var mapper = new PlayerActivityEventMapper();
        var activity = new CheckInClaimedEvent(1, "owner", 10, "2026-10", new(2026, 10, 21), Guid.NewGuid(), 1, new { Gold = 1000 })
        { PeriodKey = "2026-10-21" };
        var first = mapper.Map(activity).Message;
        Assert.Equal(first.EventId, mapper.Map(activity with { RequestId = Guid.NewGuid() }).Message.EventId);
        var originalCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("th-TH");
            Assert.Equal(first.EventId, mapper.Map(activity).Message.EventId);
        }
        finally { CultureInfo.CurrentCulture = originalCulture; }
        Assert.NotEqual(first.EventId, mapper.Map(activity with { PlayerId = 2 }).Message.EventId);
        Assert.Null(first.GameEventId);
        Assert.Null(first.EventPeriodId);
        Assert.Equal("CheckInClaimed", first.ActivityType);
    }
}
