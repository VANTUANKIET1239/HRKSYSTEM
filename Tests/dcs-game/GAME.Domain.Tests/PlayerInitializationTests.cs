using Core.Messaging.Contracts;
using Core.Persistence.Repositories;
using Core.TransactionalMessaging.Entities;
using Core.TransactionalMessaging.Inbox;
using Core.TransactionalMessaging.Outbox;
using GAME.Application.DTOs;
using GAME.Application.Events;
using GAME.Application.Features.Commands.Player;
using GAME.Application.Features.Queries.Player;
using GAME.Application.Interfaces;
using GAME.Application.Services;
using GAME.Domain.Entities;
using GAME.Infrastructure.Data;
using GAME.Infrastructure.Messaging;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GAME.Domain.Tests;

public sealed partial class PlayerInitializationTests(SummonDatabaseFixture database) : IClassFixture<SummonDatabaseFixture>
{
    [Fact]
    public void GameModelDoesNotOwnIdentityTables()
    {
        using var db = database.Context();
        Assert.DoesNotContain(db.Model.GetEntityTypes(), x => x.GetTableName() == "HRK_Users");
        Assert.DoesNotContain(db.Model.FindEntityType(typeof(HrkPlayer))!.GetForeignKeys(),
            x => x.Properties.Any(p => p.Name == nameof(HrkPlayer.UserId)));
    }

    [SummonSqlFact]
    public async Task ProvisionsCompletePlayerAndNeverRegrantsAfterInboxCleanupOrAnotherEvent()
    {
        await SeedConfigAsync();
        var account = Account();
        await RunAsync(account);
        await using var db = database.Context();
        var player = await db.Players.SingleAsync(x => x.UserId == account.UserId);
        Assert.StartsWith(account.DisplayName + " #", player.PlayerName);
        Assert.Equal(100, player.MaxExp);
        Assert.Equal(150, player.Stamina);
        var wallet = await db.PlayerWallets.SingleAsync(x => x.PlayerId == player.Id);
        Assert.Equal(4321, wallet.Gold);
        Assert.Equal(50, wallet.Diamonds);
        Assert.Equal(30, wallet.UpgradeMaterials);
        var hero = await db.PlayerHeroes.SingleAsync(x => x.PlayerId == player.Id);
        Assert.Equal(123, hero.MaxExp);
        Assert.Equal(999, hero.Power);
        Assert.False(string.IsNullOrEmpty(hero.CurrentStats));
        Assert.Single(await db.Set<HrkPlayerHeroBonusAttribute>().Where(x => x.PlayerHeroId == hero.Id).ToListAsync());
        var formation = await db.PlayerFormations.SingleAsync(x => x.PlayerId == player.Id);
        Assert.True(formation.IsSelected);
        Assert.Equal(hero.Id, formation.Position1);
        Assert.Equal(10, await db.PlayerInventories.Where(x => x.PlayerId == player.Id).SumAsync(x => x.Count));
        Assert.Equal("INITIALIZATION", (await db.HeroAcquisitionHistories.SingleAsync(x => x.PlayerId == player.Id)).SourceType);
        Assert.Equal("COMPLETED", (await db.Set<HrkPlayerInitialization>().SingleAsync(x => x.UserId == account.UserId)).Status);
        await RunAsync(account);
        await db.Set<InboxMessage>().Where(x => x.MessageId == account.EventId.ToString("N")).ExecuteDeleteAsync();
        await RunAsync(account);
        await RunAsync(account with { EventId = Guid.NewGuid() });
        Assert.Equal(1, await db.PlayerHeroes.CountAsync(x => x.PlayerId == player.Id));
        Assert.Equal(4321, (await db.PlayerWallets.AsNoTracking().SingleAsync(x => x.PlayerId == player.Id)).Gold);
        Assert.Equal(1, await db.Set<OutboxMessage>().CountAsync(x => x.PartitionKey == null && x.Payload.Contains(account.UserId)));
    }

    [SummonSqlFact]
    public async Task DifferentConcurrentDeliveriesForOneUserGrantOnce()
    {
        await SeedConfigAsync();
        var account = Account();
        await Task.WhenAll(RunAsync(account), RunAsync(account with { EventId = Guid.NewGuid() }));
        await using var db = database.Context();
        var player = await db.Players.SingleAsync(x => x.UserId == account.UserId);
        Assert.Single(await db.PlayerHeroes.Where(x => x.PlayerId == player.Id).ToListAsync());
        Assert.Single(await db.Set<HrkPlayerInitialization>().Where(x => x.UserId == account.UserId).ToListAsync());
        Assert.Equal(4321, (await db.PlayerWallets.SingleAsync(x => x.PlayerId == player.Id)).Gold);
    }

    [SummonSqlFact]
    public async Task GrantFailureRollsBackEverythingAndRetryKeepsPinnedVersion()
    {
        var originalConfig = await SeedConfigAsync();
        var account = Account();
        await Assert.ThrowsAsync<InvalidOperationException>(() => RunAsync(account, fail: true));
        await using var db = database.Context();
        Assert.False(await db.Players.AnyAsync(x => x.UserId == account.UserId));
        Assert.False(await db.Set<InboxMessage>().AnyAsync(x => x.MessageId == account.EventId.ToString("N")));
        var receipt = await db.Set<HrkPlayerInitialization>().SingleAsync(x => x.UserId == account.UserId);
        Assert.Equal("PENDING", receipt.Status);
        Assert.Equal(originalConfig, receipt.ConfigId);
        await Assert.ThrowsAnyAsync<Exception>(() => db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE dbo.HRK_PlayerInitializationConfigs SET InitialGold=99999 WHERE Id={originalConfig}"));
        await SeedConfigAsync(gold: 9000);
        await RunAsync(account);
        var player = await db.Players.SingleAsync(x => x.UserId == account.UserId);
        Assert.Equal(4321, (await db.PlayerWallets.SingleAsync(x => x.PlayerId == player.Id)).Gold);
        Assert.Equal(originalConfig, (await db.Set<HrkPlayerInitialization>().AsNoTracking().SingleAsync(x => x.UserId == account.UserId)).ConfigId);
    }

    [SummonSqlFact]
    public async Task ExistingPlayerKeepsProgressAndNonGameAccountsAreIgnored()
    {
        var existing = await database.PlayerAsync(gold: 9876);
        await RunAsync(Account() with { UserId = existing.UserId });
        await using var db = database.Context();
        Assert.Equal(9876, (await db.PlayerWallets.SingleAsync(x => x.PlayerId == existing.Id)).Gold);
        Assert.False(await db.PlayerHeroes.AnyAsync(x => x.PlayerId == existing.Id));
        var other = Account() with { AppCode = "hrm" };
        await RunAsync(other);
        Assert.False(await db.Players.AnyAsync(x => x.UserId == other.UserId));
        Assert.False(await db.Set<HrkPlayerInitialization>().AnyAsync(x => x.UserId == other.UserId));
        var query = new GetPlayerInitializationQueryHandler(new UnitOfWork<GameDbContext>(db));
        Assert.Equal("Pending", (await query.Handle(new(other.UserId), default)).Data!.Status);
        Assert.Equal("Ready", (await query.Handle(new(existing.UserId), default)).Data!.Status);
        await db.Players.Where(x => x.Id == existing.Id).ExecuteUpdateAsync(x => x.SetProperty(p => p.IsActive, false));
        Assert.Equal("Unavailable", (await query.Handle(new(existing.UserId), default)).Data!.Status);
    }

    private static AccountRegisteredV1 Account() => new(Guid.NewGuid(), Guid.NewGuid().ToString(),
        AccountContractNames.GameAppCode, "New player", DateTimeOffset.UtcNow);

    private async Task RunAsync(AccountRegisteredV1 account, bool fail = false)
    {
        await using var db = database.Context();
        var uow = new UnitOfWork<GameDbContext>(db);
        var levels = new Levels();
        var templates = database.Templates();
        var factory = new NewPlayerHeroFactory(uow, new HeroProgressionStatService(), new Power(), levels, new ZeroRandom(), templates);
        var rewards = new GameRewardGranter(uow, TestGamePlayers.Create(uow), new NoEquipment(), templates);
        IPlayerActivityEvents activities = fail ? new FailedActivity() :
            new PlayerActivityEvents(new EfOutboxWriter<GameDbContext>(db), new PlayerActivityEventMapper(), uow);
        var handler = new InitializePlayerCommandHandler(uow, new InboxExecutor<GameDbContext>(db,
            new SqlServerInboxStore<GameDbContext>(db)), levels, factory, rewards, activities, RankingTestSupport.Writer(uow), RankingTestSupport.Locks(uow));
        await handler.Handle(new(account), default);
    }

    private async Task<long> SeedConfigAsync(long gold = 4321)
    {
        await using var db = database.Context();
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        const string path = "Services/HRKGAME/GAME.Infrastructure/Data/Scripts/Migration_PlayerInitialization.sql";
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, path))) dir = dir.Parent;
        var script = (await File.ReadAllTextAsync(Path.Combine(dir!.FullName, path))).Replace("USE HRK;", "");
        foreach (var batch in System.Text.RegularExpressions.Regex.Split(script, @"(?m)^GO\s*$"))
            if (!string.IsNullOrWhiteSpace(batch)) await db.Database.ExecuteSqlRawAsync(batch);
        await db.Set<HrkPlayerInitializationConfig>().Where(x => x.IsActive).ExecuteUpdateAsync(x => x.SetProperty(p => p.IsActive, false));
        var version = (await db.Set<HrkPlayerInitializationConfig>().Select(x => (int?)x.Version).MaxAsync() ?? 0) + 1;
        var formation = new HrkFormationTemplate { Code = "INIT_" + Guid.NewGuid().ToString("N"), Name = "Starter", Slots = [new() { Slot = 1 }] };
        db.Add(formation);
        await db.SaveChangesAsync();
        var config = new HrkPlayerInitializationConfig
        {
            Version = version, IsActive = true, InitialStamina = 150, MaxStamina = 200, InitialGold = gold,
            InitialDiamonds = 50, InitialUpgradeMaterials = 30, InventoryCapacity = 200, DefaultFormationTemplateId = formation.Id
        };
        db.Add(config);
        await db.SaveChangesAsync();
        db.Add(new HrkPlayerInitializationHero { ConfigId = config.Id, HeroTemplateId = await db.HeroTemplates.Select(x => x.Id).FirstAsync(), FormationPosition = 1 });
        db.Add(new HrkPlayerInitializationItem { ConfigId = config.Id, ItemTemplateId = await db.ItemTemplates.Select(x => x.Id).FirstAsync(), Quantity = 10 });
        await db.SaveChangesAsync();
        return config.Id;
    }
}
