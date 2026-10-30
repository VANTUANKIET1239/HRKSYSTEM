using System.Text.Json;
using Core.Common.Errors;
using Core.Persistence.Repositories;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;
using GAME.Domain.Entities;
using GAME.Infrastructure.Data;
using GAME.Application.Services;
using GAME.Application.Templates;
using GAME.Infrastructure.Templates;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace GAME.Domain.Tests;

public sealed partial class SummonDatabaseFixture : IAsyncLifetime
{
    private ServiceProvider? _templateProvider;
    private readonly string name = "HRK_SummonTests_" + Guid.NewGuid().ToString("N");
    private const string Master = @"Server=(localdb)\MSSQLLocalDB;Database=master;Integrated Security=true;TrustServerCertificate=true";
    private string Connection => new SqlConnectionStringBuilder(Master)
    {
        InitialCatalog = name
    }.ConnectionString;

    public GameDbContext Context() => new SummonTestDbContext(new DbContextOptionsBuilder<GameDbContext>().UseSqlServer(Connection, sql => sql.EnableRetryOnFailure()).Options);
    public async Task InitializeAsync()
    {
        if (Environment.GetEnvironmentVariable("HRK_SUMMON_SQL_TESTS") != "1")
            return;
        await using var db = Context();
        await db.Database.EnsureCreatedAsync();
        var rarity = new HrkRarity
        {
            Code = "RARE",
            Name = "Hiếm",
            ColorHex = "#5b9bd5"
        };
        var faction = new HrkHeroFaction
        {
            Code = "TEST",
            Name = "Test"
        };
        var heroClass = new HrkHeroClass
        {
            Code = "TEST",
            Name = "Test"
        };
        var material = new HrkItemCategory
        {
            Code = "MATERIAL",
            Name = "Nguyên liệu"
        };
        var attribute = new HrkAttributeType
        {
            Code = "CRIT_RATE",
            Name = "Chí mạng",
            IsPercentage = true
        };
        var hero = new HrkHeroTemplate
        {
            Name = "Tướng thử",
            Avatar = "/hero.png",
            Rarity = rarity,
            Faction = faction,
            Class = heroClass
        };
        db.AddRange(hero, material, attribute);
        await db.SaveChangesAsync();
        db.HeroRarityUpgradeConfigs.Add(new() { RarityId = rarity.Id, StatGrowthRate = .02m, MaxLevel = 100 });
        db.HeroStarAttributePools.Add(new() { RarityId = rarity.Id, AttributeTypeId = attribute.Id, MinValue = 1, MaxValue = 2, Weight = 1, IsEnabled = true });
        await db.SaveChangesAsync();
        // Exercise the actual migration creating all four new tables and wallet RowVersion,
        // then run it again to verify it does not overwrite tuned prices or duplicate entries.
        await db.Database.ExecuteSqlRawAsync("DROP TABLE dbo.HRK_SummonResultItems; DROP TABLE dbo.HRK_SummonBatches; DROP TABLE dbo.HRK_SummonPoolEntries; DROP TABLE dbo.HRK_SummonPools; ALTER TABLE dbo.HRK_PlayerWallets DROP COLUMN RowVersion;");
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        const string relative = "Services/HRKGAME/GAME.Infrastructure/Data/Scripts/Migration_HeroSummoning.sql";
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, relative)))
            dir = dir.Parent;
        var migration = await File.ReadAllTextAsync(Path.Combine(dir!.FullName, relative));
        await db.Database.ExecuteSqlRawAsync(migration);
        await db.Database.ExecuteSqlRawAsync("UPDATE dbo.HRK_SummonPools SET SingleCost=10,TenCost=100 WHERE Code='NORMAL'; UPDATE dbo.HRK_SummonPools SET SingleCost=2,TenCost=20 WHERE Code='PREMIUM'; UPDATE dbo.HRK_ItemTemplates SET MaxStackSize=50;");
        await db.Database.ExecuteSqlRawAsync(migration);
        Assert.Equal(10, (await db.SummonPools.AsNoTracking()
            .SingleAsync(x => x.Code == "NORMAL")).SingleCost);
        Assert.Equal(2, await db.SummonPoolEntries.CountAsync());
        // Deploy the production procedures, then rerun to verify CREATE OR ALTER is safe.
        await GameOperationSqlScript.ApplyAsync(db);
        await GameOperationSqlScript.ApplyAsync(db);
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IHostEnvironment>(new CacheEnvironment());
        services.AddSingleton<IDistributedCache>(new RedisProbe());
        services.AddScoped<GameDbContext>(_ => Context());
        services.AddScoped<IUnitOfWork>(sp => new UnitOfWork<GameDbContext>(sp.GetRequiredService<GameDbContext>()));
        services.AddGameTemplateCache(new ConfigurationBuilder()
            .AddJsonFile(Path.Combine(AppContext.BaseDirectory, "appsettings.json"))
            .AddInMemoryCollection(new Dictionary<string, string?>
            { ["GameTemplateCache:Enabled"] = "false", ["GameTemplateCache:WarmupOnStartup"] = "false" }).Build());
        _templateProvider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
    }

    public async Task<HrkPlayer> PlayerAsync(long gold = 1000, int diamonds = 100, int capacity = 200)
    {
        await using var db = Context();
        var player = new HrkPlayer
        {
            UserId = Guid.NewGuid().ToString(),
            PlayerName = "Test" + Guid.NewGuid().ToString("N"),
            Wallet = new()
            {
                Gold = gold,
                Diamonds = diamonds,
                MaxCapacity = capacity
            }
        };
        db.Players.Add(player);
        await db.SaveChangesAsync();
        return player;
    }

    public IGameTemplateCatalog Templates()
    {
        var provider = _templateProvider ?? throw new InvalidOperationException("The SQL fixture is not initialized.");
        return new GameTemplateCatalog(provider.GetRequiredService<GameTemplateStore>(), provider.GetRequiredService<TemplateTableRegistry>());
    }

    public SummonHarness Service(GameDbContext db, ISummonRandom? random = null)
    {
        random ??= new ZeroRandom();
        var unitOfWork = new UnitOfWork<GameDbContext>(db);
        // Exercise configured relation hydration without caching SQL edits made by transaction tests.
        var templates = Templates();
        var grants = new SummonRewardGranter(unitOfWork, new HeroProgressionStatService(), new TestPower(), new TestLevels(), random, templates);
        return new(unitOfWork, TestGamePlayers.Create(unitOfWork), random, grants, templates);
    }

    public async Task DisposeAsync()
    {
        if (Environment.GetEnvironmentVariable("HRK_SUMMON_SQL_TESTS") != "1")
            return;
        if (_templateProvider != null) await _templateProvider.DisposeAsync();
        SqlConnection.ClearAllPools();
        await using var master = new SqlConnection(Master);
        await master.OpenAsync();
        // name is generated above, never a caller-supplied or application database name.
        await using var command = master.CreateCommand();
        command.CommandText = $"IF DB_ID('{name}') IS NOT NULL BEGIN ALTER DATABASE [{name}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{name}]; END";
        await command.ExecuteNonQueryAsync();
    }
}
