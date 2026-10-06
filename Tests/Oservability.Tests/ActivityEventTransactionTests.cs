using System.Text.RegularExpressions;
using Core.TransactionalMessaging.Entities;
using Core.TransactionalMessaging.Outbox;
using GAME.Application.Events;
using GAME.Infrastructure.Data;
using GAME.Infrastructure.Messaging;
using Microsoft.EntityFrameworkCore;
using Testcontainers.MsSql;
using Xunit;

namespace Oservability.Tests;

[Collection("Docker")]
public class ActivityEventTransactionTests
{
    [DockerFact]
    public async Task BusinessAndActivityOutbox_RollBackTogetherAndCanRetryWithSameEventId()
    {
        await using var sql = new MsSqlBuilder().Build();
        await sql.StartAsync();
        var options = new DbContextOptionsBuilder<GameDbContext>().UseSqlServer(sql.GetConnectionString()).Options;
        await using (var setup = new GameDbContext(options))
        {
            var createOutbox = Regex.Match(setup.Database.GenerateCreateScript(),
                @"CREATE TABLE (?:\[dbo\]\.)?\[HRK_OutboxMessages\] \([\s\S]+?\);", RegexOptions.CultureInvariant).Value;
            Assert.NotEmpty(createOutbox);
            await setup.Database.ExecuteSqlRawAsync(createOutbox);
            await setup.Database.ExecuteSqlRawAsync("CREATE TABLE BusinessProbe (Id int PRIMARY KEY)");
        }
        var activity = new ItemEnhancedEvent(1, "user", Guid.NewGuid(), 7, 2, 1, 2, true, 100);
        var mapper = new PlayerActivityEventMapper();
        var eventId = mapper.Map(activity).Message.EventId;
        await using (var attempt = new GameDbContext(options))
        {
            var events = new PlayerActivityEvents(new EfOutboxWriter<GameDbContext>(attempt), mapper, attempt);
            await using (var transaction = await attempt.Database.BeginTransactionAsync())
            {
                await attempt.Database.ExecuteSqlRawAsync("INSERT INTO BusinessProbe VALUES (1)");
                events.Raise(activity);
                await attempt.SaveChangesAsync();
                events.Raise(activity); // Must not insert another row after an intermediate SaveChanges.
                await attempt.SaveChangesAsync();
                Assert.Equal(eventId, (await attempt.OutboxMessages.SingleAsync()).Id);
                await transaction.RollbackAsync();
            }
            Assert.Empty(await attempt.OutboxMessages.AsNoTracking().ToListAsync());
            Assert.Equal(0, await attempt.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS Value FROM BusinessProbe").SingleAsync());
            await using (var transaction = await attempt.Database.BeginTransactionAsync())
            {
                await attempt.Database.ExecuteSqlRawAsync("INSERT INTO BusinessProbe VALUES (1)");
                // Reuse the same scoped publisher/context to exercise stale tracker recovery.
                events.Raise(activity);
                await attempt.SaveChangesAsync();
                await transaction.CommitAsync();
            }
        }
        await using var verify = new GameDbContext(options);
        var row = Assert.Single(await verify.OutboxMessages.ToListAsync());
        Assert.Equal(eventId, row.Id);
        Assert.Equal(OutboxStatus.Pending, row.Status);
        Assert.Equal(1, await verify.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS Value FROM BusinessProbe").SingleAsync());
    }
}
