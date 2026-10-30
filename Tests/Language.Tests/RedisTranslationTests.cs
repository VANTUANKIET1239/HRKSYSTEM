using System.Globalization;
using Core.Caching;
using Core.Caching.Redis;
using Core.Localization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using Xunit;

namespace Language.Tests;

public sealed class RedisTranslationTests
{
    [LanguageRedisFact]
    public async Task ConcurrentAndRepeatedPublicationsNeverDowngradeTheCurrentRevision()
    {
        var connection = Environment.GetEnvironmentVariable("HRK_LANGUAGE_REDIS_CONNECTION") ?? "localhost:6379,abortConnect=false";
        var prefix = "hrk:test:language:" + Guid.NewGuid().ToString("N") + ":";
        using var services = new ServiceCollection().AddHrkRedisConnections().BuildServiceProvider();
        var cache = new RedisTranslationCache(services.GetRequiredService<IRedisConnectionProvider>(), Options.Create(new LocalizationOptions
        { RedisConnectionString = connection, RedisKeyPrefix = prefix, CacheSeconds = 30 }));
        using var raw = await ConnectionMultiplexer.ConnectAsync(connection);
        var db = raw.GetDatabase();
        var key = (RedisKey)(prefix + "{game:en-US}:bundle");
        try
        {
            await Task.WhenAll(Enumerable.Range(1, 20).Select(value => cache.PublishAsync(Bundle(value))));
            Assert.Equal(20, (await cache.GetCurrentAsync("game", "en-US"))!.Revision);
            Assert.Equal("Hello 20", (await cache.GetCurrentAsync("game", "en-US"))!.Translations["HELLO"]);
            Assert.Equal(RedisType.Hash, await db.KeyTypeAsync(key));
            Assert.Equal(2, await db.HashLengthAsync(key));
            Assert.False(await db.KeyExistsAsync(prefix + "{game:en-US}:current"));
            Assert.False(await db.KeyExistsAsync(prefix + "{game:en-US}:revision:20"));
            await db.KeyExpireAsync(key, TimeSpan.FromSeconds(5));
            await cache.PublishAsync(Bundle(1));
            Assert.InRange((await db.KeyTimeToLiveAsync(key))!.Value.TotalSeconds, 0, 5);
            await cache.PublishAsync(Bundle(20));
            Assert.InRange((await db.KeyTimeToLiveAsync(key))!.Value.TotalSeconds, 20, 30);
            await cache.PublishAsync(Bundle(1));
            Assert.Equal(20, (await cache.GetCurrentAsync("game", "en-US"))!.Revision);
            // Verify decimal comparison at SQL BIGINT precision, beyond Lua's exact integer range.
            await cache.PublishAsync(Bundle(long.MaxValue));
            await cache.PublishAsync(Bundle(long.MaxValue - 1));
            Assert.Equal(long.MaxValue, (await cache.GetCurrentAsync("game", "en-US"))!.Revision);
            Assert.Equal(long.MaxValue.ToString(CultureInfo.InvariantCulture), (await db.HashGetAsync(key, "revision")).ToString());
            Assert.Equal("Hello " + long.MaxValue, (await cache.GetCurrentAsync("game", "en-US"))!.Translations["HELLO"]);
            await db.KeyDeleteAsync(key);
            Assert.Null(await cache.GetCurrentAsync("game", "en-US"));
            await cache.PublishAsync(Bundle(long.MaxValue));
            Assert.NotNull(await cache.GetCurrentAsync("game", "en-US"));
        }
        finally
        {
            // Delete only the exact test-created keys, never FLUSHDB or application prefixes.
            await db.KeyDeleteAsync(key);
        }
    }

    [LanguageRedisFact]
    public async Task GroupsAndCulturesAreIsolatedAndRedisWriteFailuresReachTheOutboxCaller()
    {
        var connection = Environment.GetEnvironmentVariable("HRK_LANGUAGE_REDIS_CONNECTION") ?? "localhost:6379,abortConnect=false";
        var prefix = "hrk:test:language:" + Guid.NewGuid().ToString("N") + ":";
        using var services = new ServiceCollection().AddHrkRedisConnections().BuildServiceProvider();
        var connections = services.GetRequiredService<IRedisConnectionProvider>();
        var cache = new RedisTranslationCache(connections, Options.Create(new LocalizationOptions
        { RedisConnectionString = connection, RedisKeyPrefix = prefix, CacheSeconds = 30 }));
        var db = await connections.GetDatabaseAsync(connection);
        RedisKey[] keys = [prefix + "{game:en-US}:bundle", prefix + "{game:vi-VN}:bundle", prefix + "{auth:en-US}:bundle"];
        try
        {
            Assert.Null(await cache.GetCurrentAsync("game", "en-US"));
            await cache.PublishAsync(Bundle(1));
            await cache.PublishAsync(new("game", "vi-VN", 2, new Dictionary<string, string> { ["HELLO"] = "Xin chào" }));
            await cache.PublishAsync(new("auth", "en-US", 3, new Dictionary<string, string> { ["HELLO"] = "Auth greeting" }));
            Assert.Equal("Hello 1", (await cache.GetCurrentAsync("game", "en-US"))!.Translations["HELLO"]);
            Assert.Equal("Xin chào", (await cache.GetCurrentAsync("game", "vi-VN"))!.Translations["HELLO"]);
            Assert.Equal("Auth greeting", (await cache.GetCurrentAsync("auth", "en-US"))!.Translations["HELLO"]);
            await db.KeyDeleteAsync(keys[0]);
            await db.StringSetAsync(keys[0], "wrong Redis type");
            await Assert.ThrowsAsync<RedisServerException>(() => cache.PublishAsync(Bundle(4)));
        }
        finally { await db.KeyDeleteAsync(keys); }
    }

    private static TranslationBundle Bundle(long revision) => new("game", "en-US", revision, new Dictionary<string,string> { ["HELLO"] = "Hello " + revision });
}
