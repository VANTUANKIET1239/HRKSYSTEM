using System.Text.Json;
using System.Text.RegularExpressions;
using Core.Common.Errors;
using Core.Persistence.Repositories;
using Core.TransactionalMessaging.Entities;
using Core.TransactionalMessaging.Outbox;
using GAME.Application.Common;
using GAME.Application.Events;
using GAME.Application.Features.Commands.CheckIn;
using GAME.Application.Interfaces;
using GAME.Application.Services;
using GAME.Domain.Entities;
using GAME.Infrastructure.Data;
using GAME.Infrastructure.Messaging;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GAME.Domain.Tests;

public sealed class CheckInSqlTests(SummonDatabaseFixture database) : IClassFixture<SummonDatabaseFixture>, IAsyncLifetime
{
    public async Task InitializeAsync()
    {
        if (Environment.GetEnvironmentVariable("HRK_SUMMON_SQL_TESTS") != "1") return;
        await using var db = database.Context();
        await Script(db, "Create_GameOperationProcedures.sql");
        await Script(db, "Migration_MonthlyCheckIn.sql");
    }
    public Task DisposeAsync() => Task.CompletedTask;

    [SummonSqlFact]
    public async Task ConcurrentClaimsAndRequestReplaysGrantOnceAndCommitOneOutboxEvent()
    {
        var calendar = await Calendar("2041-01");
        var player = await database.PlayerAsync();
        var clock = Clock(calendar, 21);
        await using var first = database.Context();
        await using var second = database.Context();
        var request = Guid.NewGuid();
        var results = await Task.WhenAll(Handler(first, clock).Handle(new(player.UserId, request), default),
            Handler(second, clock).Handle(new(player.UserId, Guid.NewGuid()), default));
        Assert.Single(results, x => !x.Data!.AlreadyClaimed);
        Assert.Single(results, x => x.Data!.AlreadyClaimed);
        await using var check = database.Context();
        Assert.Equal(2000, (await check.PlayerWallets.SingleAsync(x => x.PlayerId == player.Id)).Gold);
        var claim = Assert.Single(await check.Set<HrkPlayerCheckInClaim>().Where(x => x.PlayerId == player.Id).ToListAsync());
        var identity = new PlayerActivityEventMapper().Map(new CheckInClaimedEvent(player.Id, player.UserId, calendar.Id,
            calendar.MonthKey, claim.CheckInDate, claim.RequestId, 1, new { })).Message.EventId;
        Assert.Equal(1, await check.Set<OutboxMessage>().CountAsync(x => x.Id == identity));
        var replay = await Handler(check, clock).Handle(new(player.UserId, claim.RequestId), default);
        Assert.True(replay.Data!.AlreadyClaimed);
        Assert.Equal(claim.RequestId, replay.Data.RequestId);
    }

    [SummonSqlFact]
    public async Task BagFullRollsBackCurrencyAndClaim_ThenSameRequestCanSucceedAfterMakingRoom()
    {
        int materialId;
        await using (var setup = database.Context())
        {
            var material = new HrkItemTemplate
            {
                Code = "CHECKIN_TEST_ITEM", Name = "Vật phẩm điểm danh", IsStackable = true, MaxStackSize = 100,
                CategoryId = await setup.ItemCategories.Where(x => x.Code == "MATERIAL").Select(x => x.Id).FirstAsync(),
                RarityId = await setup.Rarities.Select(x => x.Id).FirstAsync()
            };
            setup.Add(material); await setup.SaveChangesAsync(); materialId = material.Id;
        }
        var reward = $"[{{\"Type\":\"GOLD\",\"Quantity\":1000}},{{\"Type\":\"ITEM\",\"ItemTemplateId\":{materialId},\"Quantity\":2}}]";
        var calendar = await Calendar("2041-02", reward);
        var player = await database.PlayerAsync(capacity: 0);
        var request = Guid.NewGuid();
        await using (var db = database.Context())
        {
            var error = await Assert.ThrowsAsync<BusinessException>(() => Handler(db, Clock(calendar, 10)).Handle(new(player.UserId, request), default));
            Assert.Equal("INVENTORY_FULL", error.Code);
        }
        await using (var check = database.Context())
        {
            var wallet = await check.PlayerWallets.SingleAsync(x => x.PlayerId == player.Id);
            Assert.Equal(1000, wallet.Gold);
            Assert.False(await check.Set<HrkPlayerCheckInClaim>().AnyAsync(x => x.PlayerId == player.Id));
            Assert.False(await check.PlayerInventories.AnyAsync(x => x.PlayerId == player.Id));
            wallet.MaxCapacity = 1; await check.SaveChangesAsync();
        }
        await using (var retry = database.Context()) await Handler(retry, Clock(calendar, 10)).Handle(new(player.UserId, request), default);
        await using var final = database.Context();
        Assert.Equal(2000, (await final.PlayerWallets.SingleAsync(x => x.PlayerId == player.Id)).Gold);
        Assert.Equal(2, (await final.PlayerInventories.SingleAsync(x => x.PlayerId == player.Id)).Count);
        Assert.Single(await final.Set<HrkPlayerCheckInClaim>().Where(x => x.PlayerId == player.Id).ToListAsync());
    }

    [SummonSqlFact]
    public async Task MissingActivityWriteRollsBackEverything()
    {
        var calendar = await Calendar("2041-03");
        var player = await database.PlayerAsync();
        await using (var db = database.Context())
            await Assert.ThrowsAsync<InvalidOperationException>(() => Handler(db, Clock(calendar, 1), new FailActivity()).Handle(new(player.UserId, Guid.NewGuid()), default));
        await using var check = database.Context();
        Assert.Equal(1000, (await check.PlayerWallets.SingleAsync(x => x.PlayerId == player.Id)).Gold);
        Assert.False(await check.Set<HrkPlayerCheckInClaim>().AnyAsync(x => x.PlayerId == player.Id));
    }

    [SummonSqlFact]
    public async Task RequestReplaySurvivesMonthRolloverAndPausedCalendar()
    {
        var january = await Calendar("2042-01");
        var february = await Calendar("2042-02");
        var player = await database.PlayerAsync();
        var request = Guid.NewGuid();
        var clock = Clock(january, 31);
        await using (var db = database.Context()) await Handler(db, clock).Handle(new(player.UserId, request), default);
        clock.UtcNow = Clock(february, 1).UtcNow;
        await using (var paused = database.Context())
        {
            var calendar = await paused.Set<HrkCheckInCalendar>().SingleAsync(x => x.Id == january.Id);
            calendar.IsActive = false; await paused.SaveChangesAsync();
        }
        await using (var replay = database.Context())
        {
            var receipt = (await Handler(replay, clock).Handle(new(player.UserId, request), default)).Data!;
            Assert.Equal(january.Id, receipt.CalendarId);
            Assert.Equal(new DateOnly(2042, 1, 31), receipt.CheckInDate);
        }
        await using (var next = database.Context())
        {
            var receipt = (await Handler(next, clock).Handle(new(player.UserId, Guid.NewGuid()), default)).Data!;
            Assert.Equal(february.Id, receipt.CalendarId);
            Assert.Equal(new DateOnly(2042, 2, 1), receipt.CheckInDate);
        }
        await using var check = database.Context();
        Assert.Equal(3000, (await check.PlayerWallets.SingleAsync(x => x.PlayerId == player.Id)).Gold);
    }

    [SummonSqlFact]
    public async Task MigrationAndPublicationAreRerunnable_AndPublishedRewardsCannotBeChanged()
    {
        await using var db = database.Context();
        // Only the random scratch database of this fixture. Exercise the actual CREATE TABLE branches as well as upgrades.
        await db.Database.ExecuteSqlRawAsync("DROP TABLE dbo.HRK_PlayerCheckInClaims; DROP TABLE dbo.HRK_CheckInRewardConfigs; DROP TABLE dbo.HRK_CheckInCalendars;");
        await Script(db, "Migration_MonthlyCheckIn.sql");
        await Script(db, "Migration_MonthlyCheckIn.sql");
        await Script(db, "Publish_MonthlyCheckInCalendar.sql");
        await Script(db, "Publish_MonthlyCheckInCalendar.sql");
        var calendar = await db.Set<HrkCheckInCalendar>().SingleAsync(x => x.MonthKey == "2026-10");
        Assert.True(calendar.IsPublished);
        Assert.Equal(31, await db.Set<HrkCheckInRewardConfig>().CountAsync(x => x.CalendarId == calendar.Id));
        await Assert.ThrowsAsync<Microsoft.Data.SqlClient.SqlException>(() => db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE dbo.HRK_CheckInRewardConfigs SET RewardsJson = N'[]' WHERE CalendarId = {calendar.Id}"));
        await Assert.ThrowsAsync<Microsoft.Data.SqlClient.SqlException>(() => db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE dbo.HRK_CheckInCalendars SET ResetTime = '04:00:00' WHERE Id = {calendar.Id}"));
        var player = await database.PlayerAsync();
        var requestId = Guid.NewGuid();
        await Handler(db, Clock(calendar, 21)).Handle(new(player.UserId, requestId), default);
        await Script(db, "Migration_MonthlyCheckIn.sql");
        Assert.Equal(requestId, (await db.Set<HrkPlayerCheckInClaim>().SingleAsync(x => x.PlayerId == player.Id)).RequestId);
    }

    [SummonSqlFact]
    public async Task InvalidRequestsAndLevelGateDoNotGrant_AndReceiptScopeIsPerPlayer()
    {
        var calendar = await Calendar("2043-01", minimumLevel: 10);
        var player = await database.PlayerAsync();
        var request = Guid.NewGuid();
        await using (var db = database.Context())
        {
            var empty = await Assert.ThrowsAsync<BusinessException>(() => Handler(db, Clock(calendar, 1)).Handle(new(player.UserId, Guid.Empty), default));
            Assert.Equal("INVALID_REQUEST_ID", empty.Code);
            var locked = await Assert.ThrowsAsync<BusinessException>(() => Handler(db, Clock(calendar, 1)).Handle(new(player.UserId, request), default));
            Assert.Equal("CHECKIN_LEVEL_REQUIRED", locked.Code);
            var trackedPlayer = await db.Players.SingleAsync(x => x.Id == player.Id);
            trackedPlayer.Level = 10; await db.SaveChangesAsync();
        }
        await using (var db = database.Context()) await Handler(db, Clock(calendar, 1)).Handle(new(player.UserId, request), default);
        var other = await database.PlayerAsync();
        await using (var db = database.Context())
        {
            var locked = await Assert.ThrowsAsync<BusinessException>(() => Handler(db, Clock(calendar, 1)).Handle(new(other.UserId, request), default));
            Assert.Equal("CHECKIN_LEVEL_REQUIRED", locked.Code);
            Assert.Equal(1000, (await db.PlayerWallets.SingleAsync(x => x.PlayerId == other.Id)).Gold);
            Assert.False(await db.Set<HrkPlayerCheckInClaim>().AnyAsync(x => x.PlayerId == other.Id));
        }
    }

    private async Task<HrkCheckInCalendar> Calendar(string month, string reward = "[{\"Type\":\"GOLD\",\"Quantity\":1000}]", int minimumLevel = 1)
    {
        var bounds = CheckInCalendarPolicy.GetMonthBounds(month, "Asia/Ho_Chi_Minh", TimeSpan.Zero);
        await using var db = database.Context();
        var calendar = new HrkCheckInCalendar { MonthKey = month, StartAtUtc = bounds.StartUtc, EndAtUtc = bounds.EndUtc, MinPlayerLevel = minimumLevel };
        db.Add(calendar); await db.SaveChangesAsync();
        for (var day = 1; day <= bounds.Days; day++) db.Add(new HrkCheckInRewardConfig { CalendarId = calendar.Id, DayOfMonth = day, RewardsJson = reward });
        await db.SaveChangesAsync(); calendar.IsPublished = true; await db.SaveChangesAsync();
        return calendar;
    }
    private static CheckInClock Clock(HrkCheckInCalendar calendar, int day) => new(calendar.StartAtUtc.AddDays(day - 1).AddMinutes(1));
    private ClaimCheckInCommandHandler Handler(GameDbContext db, TimeProvider clock, IPlayerActivityEvents? activities = null)
    {
        var unit = new UnitOfWork<GameDbContext>(db);
        var players = TestGamePlayers.Create(unit);
        return new(unit, players, new GameRewardGranter(unit, players, null!, database.Templates()),
            activities ?? new PlayerActivityEvents(new EfOutboxWriter<GameDbContext>(db), new PlayerActivityEventMapper(), unit), clock);
    }
    private sealed class FailActivity : IPlayerActivityEvents
    {
        public void Raise(PlayerActivityEvent activity) => throw new InvalidOperationException("simulated activity staging failure");
    }
    private static async Task Script(GameDbContext db, string file)
    {
        var relative = Path.Combine("Services", "HRKGAME", "GAME.Infrastructure", "Data", "Scripts", file);
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, relative))) directory = directory.Parent;
        var sql = await File.ReadAllTextAsync(Path.Combine(directory!.FullName, relative));
        await db.Database.OpenConnectionAsync();
        try
        {
            foreach (var batch in Regex.Split(sql, @"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(batch)) continue;
                await using var command = db.Database.GetDbConnection().CreateCommand();
                command.CommandText = batch;
                await command.ExecuteNonQueryAsync();
            }
        }
        finally { await db.Database.CloseConnectionAsync(); }
    }
}
