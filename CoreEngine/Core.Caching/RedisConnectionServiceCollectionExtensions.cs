using Core.Caching.Redis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Core.Caching;

public static class RedisConnectionServiceCollectionExtensions
{
    public static IServiceCollection AddHrkRedisConnections(this IServiceCollection services)
    {
        services.TryAddSingleton<IRedisConnectionProvider, RedisConnectionProvider>();
        return services;
    }
}
