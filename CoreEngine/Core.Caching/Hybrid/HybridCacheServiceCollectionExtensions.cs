using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Core.Caching.Hybrid;

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
