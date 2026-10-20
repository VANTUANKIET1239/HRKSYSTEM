using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Core.Caching.Hybrid;

public sealed class HrkHybridCacheOptions
{
    public const string SectionName = "HybridCache";
    public int LocalExpirationMinutes { get; set; } = 30;
    public int DistributedExpirationMinutes { get; set; } = 1440;
    public long MaximumPayloadBytes { get; set; } = 8 * 1024 * 1024;
}

public static class HybridCacheServiceCollectionExtensions
{
    /// <summary>Uses the existing IDistributedCache registration (Redis in GAME).</summary>
    public static IServiceCollection AddHrkHybridCache(this IServiceCollection services, IConfiguration configuration)
    {
        var options = configuration.GetSection(HrkHybridCacheOptions.SectionName)
            .Get<HrkHybridCacheOptions>() ?? new();
        if (options.LocalExpirationMinutes <= 0 || options.DistributedExpirationMinutes <= 0 || options.MaximumPayloadBytes <= 0)
            throw new InvalidOperationException("HybridCache expiration and payload limits must be positive.");
        services.AddHybridCache(cache =>
        {
            cache.MaximumPayloadBytes = options.MaximumPayloadBytes;
            cache.DefaultEntryOptions = new HybridCacheEntryOptions
            {
                Expiration = TimeSpan.FromMinutes(options.DistributedExpirationMinutes),
                LocalCacheExpiration = TimeSpan.FromMinutes(options.LocalExpirationMinutes)
            };
        });
        services.AddSingleton<IHybridCacheService, HybridCacheService>();
        return services;
    }
}

internal sealed class HybridCacheService(HybridCache cache) : IHybridCacheService
{
    public ValueTask<T> GetOrCreateAsync<T>(string key, Func<CancellationToken, ValueTask<T>> factory,
        CancellationToken cancellationToken = default, HybridCacheEntryOptions? options = null) =>
        cache.GetOrCreateAsync(key, factory, static (create, ct) => create(ct), options: options, cancellationToken: cancellationToken);

    public ValueTask SetAsync<T>(string key, T value, CancellationToken cancellationToken = default,
        HybridCacheEntryOptions? options = null) => cache.SetAsync(key, value, options, cancellationToken: cancellationToken);

    public ValueTask RemoveAsync(string key, CancellationToken cancellationToken = default) => cache.RemoveAsync(key, cancellationToken);
}
