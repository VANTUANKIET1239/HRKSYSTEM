using System.Text.Json;
using Activity.Application;
using Activity.Infrastructure;
using Core.Messaging.Contracts;
using Core.TransactionalMessaging.Inbox;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Oservability.Tests;

public sealed class ActivitySqlFactAttribute : FactAttribute
{
    public ActivitySqlFactAttribute()
    {
        if (Environment.GetEnvironmentVariable("HRK_ACTIVITY_SQL_TESTS") != "1")
            Skip = "Set HRK_ACTIVITY_SQL_TESTS=1 to run isolated SQL Server LocalDB activity tests.";
    }
}

public class EventActivitySqlTests
{
    [ActivitySqlFact]
    public async Task UpgradePreservesHistory_InboxDeduplicates_AndPeriodFiltersQueryTypedColumns()
    {
        var name = "HRK_ActivityTests_" + Guid.NewGuid().ToString("N");
        const string masterConnection = @"Server=(localdb)\MSSQLLocalDB;Database=master;Integrated Security=true;TrustServerCertificate=true";
        var connection = new SqlConnectionStringBuilder(masterConnection) { InitialCatalog = name }.ConnectionString;
        var options = new DbContextOptionsBuilder<ActivityDbContext>().UseSqlServer(connection).Options;
        try
        {
            await using (var setup = new ActivityDbContext(options))
            {
                await setup.Database.EnsureCreatedAsync();
                // Reproduce the pre-upgrade schema while retaining an existing activity.
                setup.PlayerActivities.Add(new()
                {
                    ActivityId = Guid.NewGuid(), ActivityType = "ItemEnhanced", PlayerId = 42, EntityType = "Item", EntityId = "7",
                    SourceService = "HRK.GAME", OccurredAt = DateTimeOffset.UtcNow, RecordedAt = DateTimeOffset.UtcNow
                });
                await setup.SaveChangesAsync();
                await setup.Database.ExecuteSqlRawAsync("""
                    DROP INDEX IX_PlayerActivities_PlayerId_GameEventId_PeriodKey_OccurredAt ON dbo.PlayerActivities;
                    DROP INDEX IX_PlayerActivities_EventPeriodId ON dbo.PlayerActivities;
                    DROP INDEX IX_PlayerActivities_JobExecutionId ON dbo.PlayerActivities;
                    ALTER TABLE dbo.PlayerActivities DROP COLUMN GameEventId, EventPeriodId, PeriodKey, JobExecutionId;
                    """);
                var directory = new DirectoryInfo(AppContext.BaseDirectory);
                const string relative = "Services/HRKACTIVITY/Activity.Infrastructure/Scripts/Migration_EventActivityPeriods.sql";
                while (directory is not null && !File.Exists(Path.Combine(directory.FullName, relative))) directory = directory.Parent;
                var migration = await File.ReadAllTextAsync(Path.Combine(directory!.FullName, relative));
                await setup.Database.ExecuteSqlRawAsync(migration);
                await setup.Database.ExecuteSqlRawAsync(migration);
            }
            var occurred = new DateTimeOffset(2026, 10, 20, 10, 5, 0, TimeSpan.FromHours(7));
            var id = Guid.NewGuid();
            var executionId = Guid.NewGuid();
            var message = new PlayerActivityRecordedV1(id, "PlayerEventPeriodClosed", 42, "user", "GameEvent", "3", "HRK.GAME",
                occurred, JsonSerializer.SerializeToElement(new { HighestFloor = 20 }), 3, 17, "2026-10-19", executionId);
            async Task RecordAsync(PlayerActivityRecordedV1 value)
            {
                await using var db = new ActivityDbContext(options);
                var inbox = new InboxExecutor<ActivityDbContext>(db, new SqlServerInboxStore<ActivityDbContext>(db));
                await new ActivityStore(db, inbox, NullLogger<ActivityStore>.Instance)
                    .RecordAsync(new(value, value.EventId.ToString("N"), null, null), default);
            }
            await RecordAsync(message);
            await RecordAsync(message);
            await RecordAsync(message with { EventId = Guid.NewGuid(), EventPeriodId = 18, PeriodKey = "2026-10-20", OccurredAt = occurred.AddDays(1) });
            await using var verify = new ActivityDbContext(options);
            var queries = new ActivityStore(verify, new InboxExecutor<ActivityDbContext>(verify,
                new SqlServerInboxStore<ActivityDbContext>(verify)), NullLogger<ActivityStore>.Instance);
            var filtered = await queries.GetAsync(new(PlayerId: 42, GameEventId: 3, EventPeriodId: 17,
                PeriodKey: "2026-10-19", JobExecutionId: executionId, FromUtc: occurred, ToUtc: occurred.AddDays(1)), default);
            Assert.Equal(id, Assert.Single(filtered).ActivityId);
            Assert.Equal(3, await verify.PlayerActivities.CountAsync());
            Assert.Equal(2, await verify.Set<Core.TransactionalMessaging.Entities.InboxMessage>().CountAsync());
        }
        finally
        {
            using (var pool = new SqlConnection(connection)) SqlConnection.ClearPool(pool);
            await using var master = new SqlConnection(masterConnection);
            await master.OpenAsync();
            await using var command = master.CreateCommand();
            // Only the generated scratch database above can be deleted.
            command.CommandText = $"IF DB_ID('{name}') IS NOT NULL BEGIN ALTER DATABASE [{name}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{name}]; END";
            await command.ExecuteNonQueryAsync();
        }
    }
}
