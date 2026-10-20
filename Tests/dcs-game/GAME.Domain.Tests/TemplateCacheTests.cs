using Core.Common.Errors;
using Core.Persistence.Repositories;
using GAME.Application.Features.Commands.GameConfig;
using GAME.Application.Features.Queries.Catalog;
using GAME.Application.Templates;
using GAME.Domain.Entities;
using GAME.Infrastructure.Data;
using GAME.Infrastructure.Templates;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Xunit;

namespace GAME.Domain.Tests;

public sealed class TemplateCacheTests
{
    [Fact]
    public async Task IndependentTableUsesOneKeyAndAnotherNodeReadsItWithoutDb()
    {
        var db = Seed(); var redis = new RedisProbe();
        db.Data<HrkModalConfig>().Add(new() { Id = 1, Code = "TEST", Title = "Original" });
        await using var nodeA = Node(db, redis);
        using (var scope = nodeA.CreateScope())
            Assert.Equal("Original", Assert.Single(await scope.ServiceProvider.GetRequiredService<IGameTemplateCatalog>().GetAllAsync<HrkModalConfig>()).Title);
        Assert.Equal(1, db.ReadOnlyQueries);
        Assert.EndsWith(typeof(HrkModalConfig).FullName, Assert.Single(redis.Keys).Key);
        db.Data<HrkModalConfig>()[0].Title = "Not refreshed";
        await using var nodeB = Node(db, redis);
        using var other = nodeB.CreateScope();
        Assert.Equal("Original", Assert.Single(await other.ServiceProvider.GetRequiredService<IGameTemplateCatalog>().GetAllAsync<HrkModalConfig>()).Title);
        Assert.Equal(1, db.ReadOnlyQueries);
        Assert.Single(redis.Keys);
    }

    [Fact]
    public async Task ConfigAloneControlsWhichMappedEntityCanBeCached()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        { ["GameTemplateCache:Tables:0"] = nameof(HrkModalConfig), ["GameTemplateCache:WarmupOnStartup"] = "false" }).Build();
        var db = Seed(); var redis = new RedisProbe();
        await using var node = Node(db, redis, config);
        using var scope = node.CreateScope();
        var catalog = scope.ServiceProvider.GetRequiredService<IGameTemplateCatalog>();
        Assert.Empty(await catalog.GetAllAsync<HrkModalConfig>());
        await Assert.ThrowsAsync<ArgumentException>(() => catalog.GetAllAsync<HrkHeroTemplate>());
        Assert.Equal(1, db.ReadOnlyQueries);
    }

    [Theory]
    [InlineData("DoesNotExist", null)]
    [InlineData("HrkModalConfig", "GAME.Domain.Entities.HrkModalConfig")]
    public void InvalidConfigIsRejectedBeforeAnyDatabaseQuery(string first, string? second)
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        { ["GameTemplateCache:Tables:0"] = first, ["GameTemplateCache:Tables:1"] = second }).Build();
        var db = Seed();
        using var node = Node(db, new RedisProbe(), config);
        Assert.Throws<InvalidOperationException>(() => node.GetRequiredService<TemplateTableRegistry>());
        Assert.Equal(0, db.ReadOnlyQueries);
    }

    [Fact]
    public async Task RefreshOneIndependentTableDoesNotReloadOtherTablesFromDb()
    {
        var db = Seed(); var redis = new RedisProbe();
        db.Data<HrkModalConfig>().Add(new() { Id = 1, Code = "TEST", Title = "Original" });
        await using var node = Node(db, redis);
        var refresh = node.GetRequiredService<IGameTemplateRefresh>();
        await refresh.RefreshAsync();
        var queries = db.ReadOnlyQueries;
        db.Data<HrkModalConfig>()[0].Title = "Updated";
        db.Data<HrkHeroTemplate>()[0].Name = "Not refreshed";
        await refresh.RefreshAsync<HrkModalConfig>();
        Assert.Equal(queries + 1, db.ReadOnlyQueries);
        Assert.Equal("Original", await HeroName(node));
        using var scope = node.CreateScope();
        Assert.Equal("Updated", Assert.Single(await scope.ServiceProvider.GetRequiredService<IGameTemplateCatalog>().GetAllAsync<HrkModalConfig>()).Title);
    }

    [Fact]
    public async Task FailedTableWriteCannotPublishANewRevision()
    {
        var db = Seed(); var redis = new RedisProbe();
        await using var node = Node(db, redis);
        var refresh = node.GetRequiredService<IGameTemplateRefresh>();
        await refresh.RefreshAsync();
        db.Data<HrkHeroTemplate>()[0].Name = "Not published";
        redis.FailTableWrites = true;
        await Assert.ThrowsAsync<IOException>(() => refresh.RefreshAsync());
        Assert.Equal(1, redis.RevisionWrites);
        Assert.Equal("Original", await HeroName(node));
    }

    [Fact]
    public async Task ExpiredPublishedTablesAreReloadedUnderANewRevision()
    {
        var db = Seed(); var redis = new RedisProbe();
        await using var first = Node(db, redis);
        await first.GetRequiredService<IGameTemplateRefresh>().RefreshAsync();
        foreach (var key in redis.Keys.Keys.Where(k => !k.EndsWith(":revision"))) redis.Remove(key);
        db.Data<HrkHeroTemplate>()[0].Name = "Renewed";
        await using var second = Node(db, redis);
        Assert.Equal("Renewed", await HeroName(second));
        Assert.Equal(2, redis.RevisionWrites);
    }

    [Fact]
    public async Task PinnedInitialScopeCannotLoadNewDbRowsIntoAnUncachedOldGroupAfterRefresh()
    {
        var db = Seed(); var redis = new RedisProbe();
        await using var node = Node(db, redis);
        using var pinned = node.CreateScope();
        var catalog = pinned.ServiceProvider.GetRequiredService<IGameTemplateCatalog>();
        Assert.Single(await catalog.GetAllAsync<HrkHeroTemplate>());
        db.Data<HrkModalConfig>().Add(new() { Id = 1, Code = "NEW", Title = "New revision" });
        await node.GetRequiredService<IGameTemplateRefresh>().RefreshAsync();
        await Assert.ThrowsAnyAsync<InvalidOperationException>(() => catalog.GetAllAsync<HrkModalConfig>());
    }

    [Fact]
    public async Task DisabledCacheReadsDbAndDoesNotWriteRedis()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        { ["GameTemplateCache:Enabled"] = "false", ["GameTemplateCache:Tables:0"] = nameof(HrkHeroTemplate) }).Build();
        var db = Seed(); var redis = new RedisProbe();
        await using var node = Node(db, redis, config);
        Assert.Equal("Original", await HeroName(node));
        db.Data<HrkHeroTemplate>()[0].Name = "Updated";
        Assert.Equal("Updated", await HeroName(node));
        Assert.Empty(redis.Keys);
    }

    [Fact]
    public async Task ColdRequestsCoalesceAndWarmCatalogDoesNotReadTemplateRepositories()
    {
        var db = Seed();
        var redis = new RedisProbe();
        await using var node = Node(db, redis);
        var responses = await Task.WhenAll(Enumerable.Range(0, 16).Select(async _ =>
        {
            using var scope = node.CreateScope();
            var catalog = scope.ServiceProvider.GetRequiredService<IGameTemplateCatalog>();
            var handler = new GetHeroTemplatesQueryHandler(db, catalog);
            var result = await handler.Handle(new(), default);
            Assert.Empty(scope.ServiceProvider.GetRequiredService<GameDbContext>().ChangeTracker.Entries());
            return Assert.Single(result.Data!);
        }));
        Assert.All(responses, hero => Assert.Equal("Original", hero.Name));
        Assert.All(responses, hero => Assert.Single(hero.Skills));
        var queries = db.ReadOnlyQueries;
        Assert.InRange(queries, 1, 40);
        Assert.Equal(redis.Keys.Count, queries);
        Assert.Equal(0, redis.RevisionWrites);
        using var warm = node.CreateScope();
        var catalogWarm = warm.ServiceProvider.GetRequiredService<IGameTemplateCatalog>();
        Assert.Contains(await catalogWarm.GetAllAsync<HrkHeroTemplate>(), h => h.Id == 1);
        Assert.Equal(queries, db.ReadOnlyQueries);
        Assert.Equal(0, db.SaveCount);
    }

    [Fact]
    public async Task RefreshChangesNewScopesOnBothNodesButPinsExistingScope()
    {
        var db = Seed(); var redis = new RedisProbe();
        await using var nodeA = Node(db, redis);
        await using var nodeB = Node(db, redis);
        using var pinned = nodeA.CreateScope();
        var catalog = pinned.ServiceProvider.GetRequiredService<IGameTemplateCatalog>();
        Assert.Equal("Original", ((await catalog.GetAllAsync<HrkHeroTemplate>()).Single()).Name);
        Assert.Equal("Original", await HeroName(nodeB)); // B now also has L1 for the old revision.
        db.Data<HrkHeroTemplate>()[0].Name = "Updated";
        await nodeA.GetRequiredService<IGameTemplateRefresh>().RefreshAsync();
        Assert.Equal("Original", ((await catalog.GetAllAsync<HrkHeroTemplate>()).Single()).Name);
        Assert.Equal("Updated", await HeroName(nodeA));
        Assert.Equal("Updated", await HeroName(nodeB));
    }

    [Fact]
    public async Task FailedPublicationLeavesPreviousGenerationAvailable()
    {
        var db = Seed(); var redis = new RedisProbe();
        await using var node = Node(db, redis);
        Assert.Equal("Original", await HeroName(node));
        db.Data<HrkHeroTemplate>()[0].Name = "Do not publish";
        redis.FailRevisionWrites = true;
        await Assert.ThrowsAsync<IOException>(() => node.GetRequiredService<IGameTemplateRefresh>().RefreshAsync());
        Assert.Equal("Original", await HeroName(node));
        redis.Offline = true;
        Assert.Equal("Original", await HeroName(node));
    }

    [Fact]
    public async Task DetachedGraphsAreIsolatedAndNeverContainPlayerRows()
    {
        var db = Seed(); var redis = new RedisProbe();
        await using var node = Node(db, redis);
        using (var scope = node.CreateScope())
        {
            var catalog = scope.ServiceProvider.GetRequiredService<IGameTemplateCatalog>();
            var hero = (await catalog.GetAllAsync<HrkHeroTemplate>()).Single();
            Assert.Empty(hero.PlayerHeroes);
            Assert.Equal("Faction", hero.Faction.Name);
            Assert.Equal("Skill", Assert.Single(hero.HeroSkills).Skill.Name);
            var skill = Assert.Single(hero.HeroSkills).Skill;
            var effect = Assert.Single(skill.Effects, e => e.IsActive);
            Assert.Equal("DAMAGE", effect.EffectType.Code);
            Assert.Equal("ATK", Assert.Single(effect.Scalings).AttributeType.Code);
            Assert.Equal("ATK", Assert.Single(effect.StatModifiers).AttributeType.Code);
            Assert.Equal("threshold", Assert.Single(effect.Parameters).ParameterCode);
            Assert.Equal("impact", Assert.Single(skill.AnimationConfig!.Phases).PhaseCode);
            hero.Name = "Mutated locally";
            hero.HeroSkills.Clear();
            await Assert.ThrowsAsync<ArgumentException>(() => catalog.GetAllAsync<HrkPlayer>());
        }
        Assert.Equal("Original", await HeroName(node));
        using var other = node.CreateScope();
        var otherCatalog = other.ServiceProvider.GetRequiredService<IGameTemplateCatalog>();
        Assert.Single(((await otherCatalog.GetAllAsync<HrkHeroTemplate>()).Single()).HeroSkills);
        Assert.Equal("Original", db.Data<HrkHeroTemplate>()[0].Name);
    }

    [Fact]
    public async Task ColdRedisOutageFallsBackToDbOnceAndCancellationPropagates()
    {
        var db = Seed(); var redis = new RedisProbe { Offline = true };
        await using var node = Node(db, redis);
        Assert.Equal("Original", await HeroName(node));
        var queries = db.ReadOnlyQueries;
        Assert.Equal("Original", await HeroName(node));
        Assert.Equal(queries, db.ReadOnlyQueries);
        using var cancel = new CancellationTokenSource(); cancel.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => node.GetRequiredService<IGameTemplateRefresh>().RefreshAsync(cancel.Token));
        Assert.Equal(queries, db.ReadOnlyQueries);
    }

    [Fact]
    public async Task OversizedRefreshDoesNotChangePublishedRevision()
    {
        var db = Seed(); var redis = new RedisProbe();
        await using var node = Node(db, redis);
        Assert.Equal("Original", await HeroName(node));
        db.Data<HrkHeroTemplate>()[0].Name = "Too large";
        node.GetRequiredService<IOptions<GameTemplateCacheOptions>>().Value.MaximumTableBytes = 64;
        await Assert.ThrowsAsync<InvalidOperationException>(() => node.GetRequiredService<IGameTemplateRefresh>().RefreshAsync());
        Assert.Equal("Original", await HeroName(node));
        Assert.Equal(0, redis.RevisionWrites);
    }

    [Fact]
    public async Task IncompleteTemplateRelationsCannotReplaceTheValidTables()
    {
        var db = Seed(); var redis = new RedisProbe();
        await using var node = Node(db, redis);
        Assert.Equal("Original", await HeroName(node));
        db.Data<HrkHeroFaction>().Clear();
        await Assert.ThrowsAsync<InvalidOperationException>(() => node.GetRequiredService<IGameTemplateRefresh>().RefreshAsync());
        Assert.Equal("Original", await HeroName(node));
        Assert.Equal(0, redis.RevisionWrites);
    }

    [Fact]
    public async Task UnauthorizedCommandDoesNotStartRefresh()
    {
        var refresh = new RefreshProbe();
        var handler = new RefreshTemplateCacheCommandHandler(refresh, new DeniedAccess());
        var error = await Assert.ThrowsAsync<BusinessException>(() => handler.Handle(new(), default));
        Assert.Equal(BusinessErrorKind.Forbidden, error.Kind);
        Assert.False(refresh.Called);
    }

    private static async Task<string> HeroName(IServiceProvider node)
    {
        using var scope = node.CreateScope();
        return ((await scope.ServiceProvider.GetRequiredService<IGameTemplateCatalog>().GetAllAsync<HrkHeroTemplate>()).Single()).Name;
    }

    internal static ServiceProvider Node(TowerTestStore db, IDistributedCache redis, IConfiguration? configuration = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IHostEnvironment>(new CacheEnvironment());
        services.AddSingleton(redis);
        services.AddScoped<IUnitOfWork>(_ => db);
        // Model metadata only. No connection is opened by these unit tests.
        services.AddDbContext<GameDbContext>(o => o.UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=TemplateMetadataOnly;Integrated Security=true"));
        services.AddGameTemplateCache(configuration ?? new ConfigurationBuilder().AddJsonFile(Path.Combine(AppContext.BaseDirectory, "appsettings.json")).AddInMemoryCollection(new Dictionary<string, string?>
        { ["GameTemplateCache:WarmupOnStartup"] = "false" }).Build());
        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
    }

    private static TowerTestStore Seed()
    {
        var db = new TowerTestStore();
        db.Data<HrkRarity>().Add(new() { Id = 1, Code = "R", Name = "Rare" });
        db.Data<HrkHeroFaction>().Add(new() { Id = 1, Code = "F", Name = "Faction" });
        db.Data<HrkHeroClass>().Add(new() { Id = 1, Code = "C", Name = "Class" });
        db.Data<HrkHeroTemplate>().Add(new() { Id = 1, Name = "Original", Avatar = "heroes/test.png", FactionId = 1, ClassId = 1, RarityId = 1 });
        db.Data<HrkSkillTemplate>().Add(new() { Id = "skill", Name = "Skill", Icon = "skill.svg" });
        db.Data<HrkHeroSkill>().Add(new() { HeroTemplateId = 1, SkillId = "skill" });
        db.Data<HrkAttributeType>().Add(new() { Id = 1, Code = "ATK", Name = "Attack" });
        db.Data<HrkSkillEffectType>().Add(new() { Id = 1, Code = "DAMAGE", Name = "Damage" });
        db.Data<HrkSkillTargetType>().Add(new() { Id = 1, Code = "ENEMY", Name = "Enemy" });
        db.Data<HrkSkillEffect>().Add(new() { Id = 1, SkillId = "skill", EffectTypeId = 1, TargetTypeId = 1 });
        db.Data<HrkSkillEffectScaling>().Add(new() { Id = 1, SkillEffectId = 1, AttributeTypeId = 1, Coefficient = 2 });
        db.Data<HrkSkillEffectStatModifier>().Add(new() { Id = 1, SkillEffectId = 1, AttributeTypeId = 1, Value = 5 });
        db.Data<HrkSkillEffectParameter>().Add(new() { Id = 1, SkillEffectId = 1, ParameterCode = "threshold", IntValue = 5 });
        db.Data<HrkSkillAnimationConfig>().Add(new() { Id = 1, SkillId = "skill", AnimationKey = "test" });
        db.Data<HrkSkillTimelinePhase>().Add(new() { Id = 1, SkillAnimationConfigId = 1, PhaseCode = "impact" });
        // An accidental navigation graph must never be serialized into the global snapshot.
        db.Data<HrkHeroTemplate>()[0].PlayerHeroes.Add(new() { Id = 999, PlayerId = 42 });
        return db;
    }

    private sealed class DeniedAccess : IGameTemplateRefreshAccess { public bool CanRefresh => false; }
    private sealed class RefreshProbe : IGameTemplateRefresh
    {
        public bool Called;
        public Task<TemplateRefreshResult> RefreshAsync<T>(CancellationToken cancellationToken = default) where T : class => RefreshAsync(cancellationToken);
        public Task<TemplateRefreshResult> RefreshAsync(CancellationToken cancellationToken = default)
        { Called = true; return Task.FromResult(new TemplateRefreshResult("test", DateTimeOffset.UtcNow, 1, 1)); }
    }
}

internal sealed class CacheEnvironment : Microsoft.AspNetCore.Hosting.IWebHostEnvironment
{
    public string EnvironmentName { get; set; } = "Testing";
    public string ApplicationName { get; set; } = "TemplateTests";
    public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    public string WebRootPath { get; set; } = AppContext.BaseDirectory;
    public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
}

internal sealed class RedisProbe : IDistributedCache
{
    private readonly MemoryDistributedCache _inner = new(Options.Create(new MemoryDistributedCacheOptions()));
    public bool FailRevisionWrites, FailTableWrites, Offline;
    public System.Collections.Concurrent.ConcurrentDictionary<string, byte> Keys { get; } = new();
    public int RevisionWrites;
    private void Check(CancellationToken ct) { ct.ThrowIfCancellationRequested(); if (Offline) throw new IOException("Redis offline"); }
    public byte[]? Get(string key) { Check(default); return _inner.Get(key); }
    public Task<byte[]?> GetAsync(string key, CancellationToken token = default) { Check(token); return _inner.GetAsync(key, token); }
    public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
    {
        Check(default);
        if (key.EndsWith(":revision")) { if (FailRevisionWrites) throw new IOException("Publish failed"); Interlocked.Increment(ref RevisionWrites); }
        else if (FailTableWrites) throw new IOException("Table write failed");
        _inner.Set(key, value, options);
        Keys[key] = 0;
    }
    public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
    { Check(token); Set(key, value, options); return Task.CompletedTask; }
    public void Refresh(string key) => _inner.Refresh(key);
    public Task RefreshAsync(string key, CancellationToken token = default) => _inner.RefreshAsync(key, token);
    public void Remove(string key) => _inner.Remove(key);
    public Task RemoveAsync(string key, CancellationToken token = default) => _inner.RemoveAsync(key, token);
}
