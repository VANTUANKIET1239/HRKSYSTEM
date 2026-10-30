using System.Reflection;
using Core.Caching;
using Core.Caching.Redis;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using Xunit;

namespace Core.Modules.Tests;

public sealed partial class RedisConnectionProviderTests
{
    [Fact]
    public async Task ConcurrentCallersShareOneConnectionAndCancellationDoesNotCancelOtherWaiters()
    {
        var pending = new TaskCompletionSource<IConnectionMultiplexer>(TaskCreationOptions.RunContinuationsAsynchronously);
        var calls = 0;
        using var provider = new RedisConnectionProvider(_ => { Interlocked.Increment(ref calls); return pending.Task; });
        using var cancelled = new CancellationTokenSource();
        var cancelledWaiter = provider.GetDatabaseAsync("shared", cancelled.Token);
        var waiters = Enumerable.Range(0, 20).Select(_ => provider.GetDatabaseAsync("shared")).ToArray();
        cancelled.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => cancelledWaiter);
        var database = Stub<IDatabase>((_, _) => throw new NotSupportedException());
        pending.SetResult(Stub<IConnectionMultiplexer>((method, _) => method.Name == "GetDatabase" ? database : null));
        Assert.All(await Task.WhenAll(waiters), actual => Assert.Same(database, actual));
        Assert.Same(database, await provider.GetDatabaseAsync("shared"));
        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task FailedConnectionsCanRetryAndDifferentConfigurationsRemainSeparate()
    {
        var calls = 0;
        var database = Stub<IDatabase>((_, _) => throw new NotSupportedException());
        var connection = Stub<IConnectionMultiplexer>((method, _) => method.Name == "GetDatabase" ? database : null);
        using var provider = new RedisConnectionProvider(_ => ++calls == 1
            ? Task.FromException<IConnectionMultiplexer>(new IOException("offline"))
            : Task.FromResult(connection));
        await Assert.ThrowsAsync<IOException>(() => provider.GetDatabaseAsync("first"));
        Assert.Same(database, await provider.GetDatabaseAsync("first"));
        Assert.Same(database, await provider.GetDatabaseAsync("second"));
        Assert.Same(database, await provider.GetDatabaseAsync("first"));
        Assert.Equal(3, calls);
    }

    [Fact]
    public async Task DisposalClosesCompletedAndStillConnectingConnectionsExactlyOnce()
    {
        var completedDisposals = 0;
        var pendingDisposals = 0;
        var disposedPending = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var database = Stub<IDatabase>((_, _) => throw new NotSupportedException());
        var ready = Stub<IConnectionMultiplexer>((method, _) =>
        {
            if (method.Name == "Dispose") Interlocked.Increment(ref completedDisposals);
            return method.Name == "GetDatabase" ? database : null;
        });
        var pending = new TaskCompletionSource<IConnectionMultiplexer>(TaskCreationOptions.RunContinuationsAsynchronously);
        var provider = new RedisConnectionProvider(configuration => configuration == "ready" ? Task.FromResult(ready) : pending.Task);
        await provider.GetDatabaseAsync("ready");
        using var cancelled = new CancellationTokenSource();
        var abandoned = provider.GetDatabaseAsync("pending", cancelled.Token);
        cancelled.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => abandoned);
        provider.Dispose();
        provider.Dispose();
        pending.SetResult(Stub<IConnectionMultiplexer>((method, _) =>
        {
            if (method.Name == "Dispose") { Interlocked.Increment(ref pendingDisposals); disposedPending.TrySetResult(); }
            return null;
        }));
        await disposedPending.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(1, completedDisposals);
        Assert.Equal(1, pendingDisposals);
        await Assert.ThrowsAsync<ObjectDisposedException>(() => provider.GetDatabaseAsync("ready"));
    }

    [Fact]
    public void RegistrationIsSingletonAndDoesNotRequireARedisConnection()
    {
        var services = new ServiceCollection();
        services.AddHrkRedisConnections();
        services.AddHrkRedisConnections();
        using var root = services.BuildServiceProvider();
        using var first = root.CreateScope();
        using var second = root.CreateScope();
        Assert.Single(services, x => x.ServiceType == typeof(IRedisConnectionProvider));
        Assert.Same(first.ServiceProvider.GetRequiredService<IRedisConnectionProvider>(),
            second.ServiceProvider.GetRequiredService<IRedisConnectionProvider>());
    }

    private static T Stub<T>(Func<MethodInfo, object?[]?, object?> handler) where T : class
    {
        var stub = DispatchProxy.Create<T, RedisStub>();
        ((RedisStub)(object)stub).Handler = handler;
        return stub;
    }
}
