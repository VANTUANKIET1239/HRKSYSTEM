using System.Reflection;
using System.Text.Json;
using Core.Caching.Redis;
using Core.Localization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using Xunit;

namespace Language.Tests;

public sealed class RedisTranslationCacheTests
{
    [Fact]
    public async Task ACurrentBundleIsReadWithOneRedisCommand()
    {
        var calls = 0;
        var bundle = new TranslationBundle("game", "en-US", long.MaxValue,
            new Dictionary<string, string> { ["HELLO"] = "Hello" });
        var db = Database((method, args) =>
        {
            calls++;
            Assert.Equal("HashGetAsync", method.Name);
            Assert.Equal("test:{game:en-US}:bundle", ((RedisKey)args![0]!).ToString());
            Assert.Equal("payload", ((RedisValue)args[1]!).ToString());
            return Task.FromResult((RedisValue)JsonSerializer.Serialize(bundle, new JsonSerializerOptions(JsonSerializerDefaults.Web)));
        });
        var result = await Cache(db).GetCurrentAsync("game", "en-US");
        Assert.Equal(long.MaxValue, result!.Revision);
        Assert.Equal("Hello", result.Translations["HELLO"]);
        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task EmptyOrMismatchedPayloadIsACacheMiss()
    {
        var payload = RedisValue.Null;
        var cache = Cache(Database((_, _) => Task.FromResult(payload)));
        Assert.Null(await cache.GetCurrentAsync("game", "en-US"));
        payload = """{"group":"auth","culture":"en-US","revision":1,"translations":{}}""";
        Assert.Null(await cache.GetCurrentAsync("game", "en-US"));
        payload = """{"group":"game","culture":"vi-VN","revision":1,"translations":{}}""";
        Assert.Null(await cache.GetCurrentAsync("game", "en-US"));
        payload = """{"group":"game","culture":"en-US","revision":0,"translations":{}}""";
        Assert.Null(await cache.GetCurrentAsync("game", "en-US"));
    }

    [Fact]
    public async Task RedisFailuresReachReadersAndOutboxPublishers()
    {
        var cache = Cache(Database((method, _) => method.Name switch
        {
            "HashGetAsync" => Task.FromException<RedisValue>(new IOException("Redis offline")),
            "ScriptEvaluateAsync" => Task.FromException<RedisResult>(new IOException("Redis write failed")),
            _ => throw new NotSupportedException(method.Name)
        }));
        await Assert.ThrowsAsync<IOException>(() => cache.GetCurrentAsync("game", "en-US"));
        await Assert.ThrowsAsync<IOException>(() => cache.PublishAsync(new("game", "en-US", 1, new Dictionary<string, string>())));
    }

    [Fact]
    public void RegistrationInjectsTheProviderWithoutConnectingDuringResolution()
    {
        var services = new ServiceCollection();
        services.AddHrkLocalization(new ConfigurationBuilder().Build());
        using var root = services.BuildServiceProvider();
        using var scope = root.CreateScope();
        Assert.IsType<RedisTranslationCache>(root.GetRequiredService<ITranslationCache>());
        Assert.Same(root.GetRequiredService<IRedisConnectionProvider>(), scope.ServiceProvider.GetRequiredService<IRedisConnectionProvider>());
        Assert.Same(root.GetRequiredService<ITranslationCache>(), scope.ServiceProvider.GetRequiredService<ITranslationCache>());
    }

    private static RedisTranslationCache Cache(IDatabase database) => new(new Connection(database),
        Options.Create(new LocalizationOptions { RedisKeyPrefix = "test:" }));

    private sealed class Connection(IDatabase database) : IRedisConnectionProvider
    {
        public Task<IDatabase> GetDatabaseAsync(string configuration, CancellationToken cancellationToken = default)
        { cancellationToken.ThrowIfCancellationRequested(); return Task.FromResult(database); }
    }

    private static IDatabase Database(Func<MethodInfo, object?[]?, object?> handler)
    {
        var database = DispatchProxy.Create<IDatabase, DatabaseStub>();
        ((DatabaseStub)database).Handler = handler;
        return database;
    }

    public class DatabaseStub : DispatchProxy
    {
        public Func<MethodInfo, object?[]?, object?> Handler { get; set; } = null!;
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => Handler(targetMethod!, args);
    }
}
