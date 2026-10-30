using System.Text.Json;
using Core.Persistence.Repositories;
using Core.Scheduling;
using Core.TransactionalMessaging.Entities;
using Core.TransactionalMessaging.Outbox;
using GAME.Application.Events;
using GAME.Application.Interfaces;
using GAME.Application.Services;
using GAME.Domain.Entities;
using GAME.Infrastructure.Data;
using GAME.Infrastructure.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Quartz;
using GAME.Infrastructure.Scheduling;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace GAME.Domain.Tests;

public partial class EventResetSqlTests(SummonDatabaseFixture database) : IClassFixture<SummonDatabaseFixture>
{
    [SummonSqlFact]
    public async Task SettlementAndOutbox_RollBackTogether_AndConcurrentRetriesCloseThePeriodOnce()
    {
        var player = await database.PlayerAsync();
        HrkEventPeriod period;
        HrkGameEvent gameEvent;
        await using (var setup = database.Context())
        {
            gameEvent = new() { Code = "RESET_" + Guid.NewGuid().ToString("N"), Name = "Reset test", BannerImagePath = "" };
            period = new()
            {
                Event = gameEvent, PeriodKey = "2026-10-18", StartAtUtc = new(2026, 10, 18, 5, 0, 0), EndAtUtc = new(2026, 10, 19, 5, 0, 0),
                ChestSnapshotJson = JsonSerializer.Serialize(new[] { new EventChestSnapshot(7, 10, "[{\"Type\":\"GOLD\",\"Quantity\":5}]") })
            };
            setup.Add(new HrkPlayerEventPeriodProgress { PlayerId = player.Id, Event = gameEvent, EventPeriod = period, PeriodKey = period.PeriodKey, HighestFloorInPeriod = 20 });
            await setup.SaveChangesAsync();
        }
        var cutoff = new DateTime(2026, 10, 20, 5, 0, 0, DateTimeKind.Utc);
        var expectedActivityId = new PlayerActivityEventMapper().Map(new GameEventActivityEvent(
            player.Id, player.UserId, "PlayerEventPeriodClosed", $"period:{period.Id}", new { })).Message.EventId;
        await using (var attempt = database.Context())
        {
            await using var uow = new UnitOfWork<GameDbContext>(attempt);
            var writer = new EfOutboxWriter<GameDbContext>(attempt);
            var events = new PlayerActivityEvents(writer, new(), uow);
            var settlement = new TowerEventService(uow, events, writer, new EventResetSchedule(uow));
            await Assert.ThrowsAsync<InvalidOperationException>(() => new TowerOperationRunner(uow, RankingTestSupport.Writer(uow), RankingTestSupport.Locks(uow)).RunAsync<bool>(player.UserId, async () =>
            {
                await settlement.CloseExpiredPeriodsAsync(player.Id, gameEvent, cutoff, Guid.NewGuid(), default);
                throw new InvalidOperationException("Injected failure after the intermediate SaveChanges.");
            }, default));
        }
        await using (var check = database.Context())
        {
            Assert.Null((await check.PlayerEventPeriodProgresses.SingleAsync(x => x.EventPeriodId == period.Id)).ClosedAtUtc);
            Assert.False(await check.PlayerPendingRewards.AnyAsync(x => x.EventPeriodId == period.Id));
            Assert.False(await check.OutboxMessages.AnyAsync(x => x.Id == expectedActivityId));
        }
        async Task<int> CloseAsync()
        {
            await using var db = database.Context();
            await using var uow = new UnitOfWork<GameDbContext>(db);
            var writer = new EfOutboxWriter<GameDbContext>(db);
            var events = new PlayerActivityEvents(writer, new(), uow);
            var settlement = new TowerEventService(uow, events, writer, new EventResetSchedule(uow));
            return await new TowerOperationRunner(uow, RankingTestSupport.Writer(uow), RankingTestSupport.Locks(uow)).RunAsync(player.UserId,
                () => settlement.CloseExpiredPeriodsAsync(player.Id, gameEvent, cutoff, Guid.NewGuid(), default), default);
        }
        var closed = await Task.WhenAll(CloseAsync(), CloseAsync());
        Assert.Equal(1, closed.Sum());
        await using var verify = database.Context();
        Assert.Single(await verify.PlayerPendingRewards.Where(x => x.EventPeriodId == period.Id).ToListAsync());
        Assert.NotNull((await verify.PlayerEventPeriodProgresses.SingleAsync(x => x.EventPeriodId == period.Id)).ClosedAtUtc);
        var activity = Assert.Single(await verify.OutboxMessages.Where(x => x.Id == expectedActivityId).ToListAsync());
        Assert.Equal(OutboxStatus.Pending, activity.Status);
    }

    private const string ActivityContractName = "game.activity.recorded.v1";

    [SummonSqlFact]
    public async Task ManagedGameHandler_ClosesOfflinePlayers_AndApiResolvesTheSamePeriod()
    {
        var player = await database.PlayerAsync();
        HrkGameEvent gameEvent;
        long oldPeriodId;
        await using (var setup = database.Context())
        {
            gameEvent = new()
            {
                Code = "OFFLINE_" + Guid.NewGuid().ToString("N"), Name = "Offline reset", BannerImagePath = "",
                ResetTime = TimeSpan.FromHours(12), TimeZoneId = "UTC"
            };
            var old = new HrkEventPeriod
            {
                Event = gameEvent, PeriodKey = DateTime.UtcNow.AddDays(-2).ToString("yyyy-MM-dd"),
                StartAtUtc = DateTime.UtcNow.Date.AddDays(-2), EndAtUtc = DateTime.UtcNow.Date.AddDays(-1),
                ChestSnapshotJson = JsonSerializer.Serialize(new[] { new EventChestSnapshot(7, 10, "[{\"Type\":\"GOLD\",\"Quantity\":5}]") })
            };
            setup.Add(new HrkPlayerEventPeriodProgress { PlayerId = player.Id, Event = gameEvent, EventPeriod = old, PeriodKey = old.PeriodKey, HighestFloorInPeriod = 20 });
            await setup.SaveChangesAsync();
            oldPeriodId = old.Id;
        }
        var services = new ServiceCollection().AddLogging();
        services.AddScoped<GameDbContext>(_ => database.Context());
        services.AddScoped<IUnitOfWork>(sp => new UnitOfWork<GameDbContext>(sp.GetRequiredService<GameDbContext>()));
        services.AddScoped<IOutboxWriter, EfOutboxWriter<GameDbContext>>();
        services.AddSingleton<PlayerActivityEventMapper>();
        services.AddScoped<IPlayerActivityEvents, PlayerActivityEvents>();
        services.AddScoped<GAME.Application.Interfaces.IPlayerRankingWriter>(sp => RankingTestSupport.Writer(sp.GetRequiredService<IUnitOfWork>()));
        services.AddScoped<TowerOperationRunner>();
        services.AddScoped<GAME.Application.Interfaces.IGameTransactionLock>(sp => RankingTestSupport.Locks(sp.GetRequiredService<IUnitOfWork>()));
        services.AddScoped<ITowerEventService, TowerEventService>();
        services.AddGameScheduling(new ConfigurationBuilder().Build());
        await using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        long definitionId;
        await using (var scope = provider.CreateAsyncScope())
        {
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var eventScope = gameEvent.Id.ToString();
            Assert.False(await unitOfWork.ReadOnlyRepository<JobDefinition>().Query().AnyAsync(x => x.ScopeKey == eventScope));
            var definition = new JobDefinition
            {
                JobCode = $"EVENT_RESET:{gameEvent.Id}", HandlerKey = EventResetJobHandler.Key,
                ScopeKey = eventScope, CronExpression = "0 0 0 * * ?", TimeZoneId = "UTC"
            };
            await unitOfWork.Repository<JobDefinition>().AddAsync(definition);
            await unitOfWork.SaveChangesAsync();
            definitionId = definition.Id;
        }
        var executionRunner = new JobExecutionRunner(provider.GetRequiredService<IServiceScopeFactory>(),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<JobExecutionRunner>.Instance);
        await executionRunner.RunAsync(definitionId, DateTime.UtcNow, "test-game", default);
        await executionRunner.RunAsync(definitionId, DateTime.UtcNow, "test-game", default);
        await using var verifyScope = provider.CreateAsyncScope();
        var verify = verifyScope.ServiceProvider.GetRequiredService<GameDbContext>();
        var executions = await verify.Set<JobExecution>().Where(x => x.JobDefinitionId == definitionId).OrderBy(x => x.StartedAtUtc).ToListAsync();
        Assert.Equal(2, executions.Count);
        Assert.All(executions, x => Assert.Equal("SUCCEEDED", x.Status));
        var progress = await verify.PlayerEventPeriodProgresses.SingleAsync(x => x.EventPeriodId == oldPeriodId);
        Assert.Equal(executions[0].Id, progress.ResetExecutionId);
        Assert.Single(await verify.PlayerPendingRewards.Where(x => x.EventPeriodId == oldPeriodId).ToListAsync());
        var apiPeriod = await verifyScope.ServiceProvider.GetRequiredService<ITowerEventService>().ReadCurrentPeriodAsync(gameEvent);
        Assert.Equal(DateTime.UtcNow.Date, apiPeriod.StartAtUtc);
        Assert.NotEqual(0, apiPeriod.Id);
        var payloads = await verify.OutboxMessages.Where(x => x.EventName == ActivityContractName).Select(x => x.Payload).ToListAsync();
        var activityMessages = payloads.Select(x => JsonSerializer.Deserialize<Core.Messaging.Contracts.PlayerActivityRecordedV1>(
            x, new JsonSerializerOptions(JsonSerializerDefaults.Web))!);
        Assert.Equal(2, activityMessages.Count(x => x.GameEventId == gameEvent.Id &&
            x.ActivityType is "PlayerEventPeriodClosed" or "EventResetCompleted"));
    }

    [SummonSqlFact]
    public async Task MigrationsAreRerunnable_AndPersistentQuartzExecutesWithSqlHistory()
    {
        string connection;
        long jobId;
        await using (var setup = database.Context())
        {
            connection = setup.Database.GetConnectionString()!;
            var migration = await ReadAsync("Services/HRKGAME/GAME.Infrastructure/Data/Scripts/Migration_EventScheduling.sql");
            var selected = new HrkGameEvent { Code = "EXPLICIT_" + Guid.NewGuid().ToString("N"), Name = "Explicit job", BannerImagePath = "" };
            var unregistered = new HrkGameEvent { Code = "UNREGISTERED_" + Guid.NewGuid().ToString("N"), Name = "No job", BannerImagePath = "" };
            setup.AddRange(selected, unregistered);
            await setup.SaveChangesAsync();
            await ExecuteScriptAsync(setup, migration);
            await ExecuteScriptAsync(setup, migration);
            var selectedScope = selected.Id.ToString();
            var unregisteredScope = unregistered.Id.ToString();
            Assert.False(await setup.Set<JobDefinition>().AnyAsync(x => x.ScopeKey == selectedScope || x.ScopeKey == unregisteredScope));
            var registration = (await ReadAsync("Services/HRKGAME/GAME.Infrastructure/Data/Scripts/Register_TowerDailyResetJob.sql"))
                .Replace("N'TOWER_CLIMB'", "N'" + selected.Code + "'");
            await ExecuteScriptAsync(setup, registration);
            var explicitJob = await setup.Set<JobDefinition>().SingleAsync(x => x.HandlerKey == EventResetJobHandler.Key && x.ScopeKey == selectedScope);
            explicitJob.TimeoutSeconds = 456;
            await setup.SaveChangesAsync();
            await ExecuteScriptAsync(setup, registration);
            setup.ChangeTracker.Clear();
            Assert.Equal(456, (await setup.Set<JobDefinition>().SingleAsync(x => x.ScopeKey == selectedScope)).TimeoutSeconds);
            Assert.False(await setup.Set<JobDefinition>().AnyAsync(x => x.ScopeKey == unregisteredScope));
            var schema = await ReadAsync("CoreEngine/Core.Scheduling/Scripts/CreateQuartzSqlServer.sql");
            await ExecuteScriptAsync(setup, schema);
            await ExecuteScriptAsync(setup, schema);
            foreach (var existing in await setup.Set<JobDefinition>().ToListAsync()) existing.IsEnabled = false;
            var definition = new JobDefinition
            {
                JobCode = "SQL_PROBE_" + Guid.NewGuid().ToString("N"), HandlerKey = "SQL_PROBE",
                ScopeKey = Guid.NewGuid().ToString("N"), CronExpression = "0 0 0 1 1 ? 2099", RecoveryIntervalSeconds = 0
            };
            setup.Add(definition);
            await setup.SaveChangesAsync();
            jobId = definition.Id;
        }
        var state = new ProbeState();
        var services = new ServiceCollection().AddLogging();
        services.AddScoped<GameDbContext>(_ => database.Context());
        services.AddScoped<IUnitOfWork>(sp => new UnitOfWork<GameDbContext>(sp.GetRequiredService<GameDbContext>()));
        services.AddSingleton(state);
        services.AddScoped<IScheduledJobHandler, ProbeHandler>();
        services.AddManagedScheduling(options =>
        {
            options.Enabled = true; options.ConnectionString = connection;
            options.SchedulerName = "sql-test-" + Guid.NewGuid().ToString("N");
        });
        await using var provider = services.BuildServiceProvider();
        var scheduler = await provider.GetRequiredService<ISchedulerFactory>().GetScheduler();
        try
        {
            await scheduler.Start();
            await provider.GetRequiredService<IJobScheduleManager>().SynchronizeAsync();
            await state.Completed.Task.WaitAsync(TimeSpan.FromSeconds(20));
        }
        finally { await scheduler.Shutdown(true); }
        await using var verify = database.Context();
        var execution = Assert.Single(await verify.Set<JobExecution>().Where(x => x.JobDefinitionId == jobId).ToListAsync());
        Assert.Equal("SUCCEEDED", execution.Status);
        Assert.Equal(1, execution.Attempts);
        Assert.NotNull(execution.CompletedAtUtc);
    }
    private static async Task<string> ReadAsync(string relative)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, relative))) directory = directory.Parent;
        return await File.ReadAllTextAsync(Path.Combine(directory!.FullName, relative));
    }

    private static async Task ExecuteScriptAsync(DbContext db, string sql)
    {
        await db.Database.OpenConnectionAsync();
        try
        {
            await using var command = db.Database.GetDbConnection().CreateCommand();
            command.CommandText = sql;
            await command.ExecuteNonQueryAsync();
        }
        finally { await db.Database.CloseConnectionAsync(); }
    }
}
