using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Core.Caching
{
    public static class RedisCacheServiceCollectionExtensions
    {
        /// <summary>Registers the Redis IDistributedCache backend used by HybridCache and revision markers.</summary>
        public static IServiceCollection AddHrkRedisCache(this IServiceCollection services, IConfiguration configuration)
        {
            var section = configuration.GetSection(RedisCacheOptions.SectionName);
            var options = section.Get<RedisCacheOptions>() ?? new RedisCacheOptions();
            services.AddStackExchangeRedisCache(redis =>
            {
                redis.Configuration = options.ConnectionString;
                redis.InstanceName = options.InstanceName;
            });
            return services;
        }
    }
}
