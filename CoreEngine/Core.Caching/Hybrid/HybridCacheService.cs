using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Core.Caching.Hybrid;

internal sealed class HybridCacheService(HybridCache cache) : IHybridCacheService
{
    public ValueTask<T> GetOrCreateAsync<T>(string key, Func<CancellationToken, ValueTask<T>> factory,
        CancellationToken cancellationToken = default, HybridCacheEntryOptions? options = null) =>
        cache.GetOrCreateAsync(key, factory, static (create, ct) => create(ct), options: options, cancellationToken: cancellationToken);

    public ValueTask SetAsync<T>(string key, T value, CancellationToken cancellationToken = default,
        HybridCacheEntryOptions? options = null) => cache.SetAsync(key, value, options, cancellationToken: cancellationToken);

    public ValueTask RemoveAsync(string key, CancellationToken cancellationToken = default) => cache.RemoveAsync(key, cancellationToken);
}
