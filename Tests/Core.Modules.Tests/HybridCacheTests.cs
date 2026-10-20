using Core.Caching.Hybrid;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Core.Modules.Tests;

public sealed class HybridCacheTests
{
    [Fact]
    public async Task ConcurrentMissesUseOneFactoryAndAnotherNodeReadsL2()
    {
        var distributed = new DistributedBackend();
        await using var first = Node(distributed);
        await using var second = Node(distributed);
        var cache = first.GetRequiredService<IHybridCacheService>();
        var key = Guid.NewGuid().ToString("N");
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var factoryCalls = 0;
        var requests = Enumerable.Range(0, 20).Select(_ => cache.GetOrCreateAsync(key, async ct =>
        {
            Interlocked.Increment(ref factoryCalls);
            await gate.Task.WaitAsync(ct);
            return "payload";
        }).AsTask()).ToArray();
        gate.SetResult();
        Assert.All(await Task.WhenAll(requests), value => Assert.Equal("payload", value));
        Assert.Equal(1, factoryCalls);
        var fromOtherNode = await second.GetRequiredService<IHybridCacheService>().GetOrCreateAsync<string>(key,
            _ => throw new InvalidOperationException("L2 should supply the value."));
        Assert.Equal("payload", fromOtherNode);
    }

    [Fact]
    public async Task CancellationIsNotConvertedToACacheMiss()
    {
        await using var node = Node(new DistributedBackend());
        using var source = new CancellationTokenSource(); source.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await node.GetRequiredService<IHybridCacheService>().GetOrCreateAsync("cancelled",
                ct => { ct.ThrowIfCancellationRequested(); return ValueTask.FromResult("unused"); }, source.Token));
    }

    private static ServiceProvider Node(IDistributedCache cache)
    {
        var services = new ServiceCollection(); services.AddLogging(); services.AddSingleton(cache);
        services.AddHrkHybridCache(new ConfigurationBuilder().Build());
        return services.BuildServiceProvider();
    }

    // HybridCache deliberately bypasses the exact MemoryDistributedCache type.
    // A distinct backend type lets this test exercise its real L2 serialization path.
    private sealed class DistributedBackend() : MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
}
