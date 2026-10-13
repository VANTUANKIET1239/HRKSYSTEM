using Core.Caching;
using Core.Persistence.Repositories;
using GAME.Application.DTOs;
using GAME.Application.Features.Queries.GameConfig;
using GAME.Domain.Entities;
using GAME.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GAME.Domain.Tests;

public class ModalConfigTests
{
    [Fact]
    public async Task QueryReturnsOnlyActivePresetsWithoutSavingAndCachesThemForFiveMinutes()
    {
        var store = new TowerTestStore();
        store.Data<HrkModalConfig>().AddRange([
            new() { Code="LOGOUT_CONFIRM", Kind="CONFIRMATION", Title="Đăng xuất", Revision=3 },
            new() { Code="DISABLED", Title="Inactive", IsActive=false }
        ]);
        var cache = new ModalConfigCacheProbe();
        var handler = new GetModalConfigsQueryHandler(store, cache);
        var response = await handler.Handle(new(), default);
        Assert.Equal("LOGOUT_CONFIRM", Assert.Single(response.Data!).Code);
        Assert.Equal(3, response.Data![0].Revision);
        Assert.Equal(GetModalConfigsQueryHandler.CacheKey, cache.LastKey);
        Assert.Equal(TimeSpan.FromMinutes(5), cache.Expiry);
        Assert.Equal(1, cache.Writes);
        Assert.Equal(0, store.SaveCount);

        store.Data<HrkModalConfig>().Clear();
        var cached = await handler.Handle(new(), default);
        Assert.Equal("LOGOUT_CONFIRM", Assert.Single(cached.Data!).Code);
        Assert.Equal(1, cache.Writes);
    }

    [Fact]
    public async Task EmptyConfigIsCachedAndStillReturnsSuccessForFrontendFallbacks()
    {
        var cache = new ModalConfigCacheProbe();
        var handler = new GetModalConfigsQueryHandler(new TowerTestStore(), cache);
        var response = await handler.Handle(new(), default);
        Assert.True(response.Success);
        Assert.Empty(response.Data!);
        await handler.Handle(new(), default);
        Assert.Equal(1, cache.Writes);
    }

    [Fact]
    public async Task RedisMissAfterOutageStillReturnsDatabaseConfig()
    {
        // The existing Redis adapter fails open: GET returns null and SET is a no-op during outages.
        var store = new TowerTestStore();
        store.Data<HrkModalConfig>().Add(new() { Code="BATTLE_RESULT", Title="Kết quả trận đấu" });
        var response = await new GetModalConfigsQueryHandler(store, new ModalConfigCacheProbe { Available=false })
            .Handle(new(), default);
        Assert.Equal("Kết quả trận đấu", Assert.Single(response.Data!).Title);
    }

    [Fact]
    public async Task PreCancelledQueryDoesNotTouchTheCache()
    {
        var cache = new ModalConfigCacheProbe();
        using var source = new CancellationTokenSource();
        source.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            new GetModalConfigsQueryHandler(new TowerTestStore(), cache).Handle(new(), source.Token));
        Assert.Equal(0, cache.Reads);
    }
}

internal sealed class ModalConfigCacheProbe : ICacheService
{
    private object? _value;
    public bool Available = true;
    public int Reads, Writes;
    public string? LastKey;
    public TimeSpan? Expiry;
    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        Reads++;
        LastKey = key;
        return Task.FromResult(Available && _value is T value ? value : default);
    }
    public Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken cancellationToken = default)
    {
        Writes++;
        LastKey = key;
        Expiry = expiry;
        if (Available) _value = value;
        return Task.CompletedTask;
    }
    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        _value = null;
        return Task.CompletedTask;
    }
}

public class ModalConfigSqlTests(SummonDatabaseFixture database) : IClassFixture<SummonDatabaseFixture>
{
    [SummonSqlFact]
    public async Task ProductionMigrationCreatesAndSeedsTheTableAndPreservesEditsOnRerun()
    {
        await using var db = database.Context();
        await using var uow = new UnitOfWork<GameDbContext>(db);
        // EnsureCreated includes the new model. Drop only the new table in this isolated fixture DB
        // to exercise the actual CREATE TABLE migration as well as rerunnable seeding.
        await db.Database.ExecuteSqlRawAsync("DROP TABLE dbo.HRK_ModalConfigs");
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        const string relative = "Services/HRKGAME/GAME.Infrastructure/Data/Scripts/Migration_ModalConfigs.sql";
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, relative))) directory = directory.Parent;
        var script = await File.ReadAllTextAsync(Path.Combine(directory!.FullName, relative));
        await uow.Sql.ExecuteAsync(script);
        Assert.Equal(26, await db.ModalConfigs.CountAsync());
        var edited = await db.ModalConfigs.SingleAsync(config => config.Code == "LOGOUT_CONFIRM");
        edited.Title = "Tiêu đề đã chỉnh sửa";
        edited.IsActive = false;
        edited.Revision = 4;
        await db.SaveChangesAsync();
        await uow.Sql.ExecuteAsync(script);
        db.ChangeTracker.Clear();
        var saved = await db.ModalConfigs.AsNoTracking().SingleAsync(config => config.Code == "LOGOUT_CONFIRM");
        Assert.Equal("Tiêu đề đã chỉnh sửa", saved.Title);
        Assert.False(saved.IsActive);
        Assert.Equal(4, saved.Revision);
        Assert.Equal(26, await db.ModalConfigs.CountAsync());

        var response = await new GetModalConfigsQueryHandler(uow, new ModalConfigCacheProbe()).Handle(new(), default);
        Assert.Equal(25, response.Data!.Count);
        Assert.DoesNotContain(response.Data!, config => config.Code == "LOGOUT_CONFIRM");
        Assert.Contains(response.Data!, config => config.Code == "TOWER_START_QUICK_CLIMB" && config.MessageTemplate!.Contains("{limit}"));
        Assert.Empty(db.ChangeTracker.Entries());
    }
}
