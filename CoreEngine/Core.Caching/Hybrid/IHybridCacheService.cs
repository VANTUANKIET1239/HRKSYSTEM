using Microsoft.Extensions.Caching.Hybrid;

namespace Core.Caching.Hybrid;

/// <summary>Typed L1/L2 caching. The factory must not capture a request's mutable state.</summary>
public interface IHybridCacheService
{
    ValueTask<T> GetOrCreateAsync<T>(string key, Func<CancellationToken, ValueTask<T>> factory,
        CancellationToken cancellationToken = default, HybridCacheEntryOptions? options = null);
    ValueTask SetAsync<T>(string key, T value, CancellationToken cancellationToken = default,
        HybridCacheEntryOptions? options = null);
    ValueTask RemoveAsync(string key, CancellationToken cancellationToken = default);
}
