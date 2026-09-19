using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Core.Common.Caching
{
    public static class RedisCacheServiceCollectionExtensions
    {
        public static IServiceCollection AddHrkRedisCache(this IServiceCollection services, IConfiguration configuration)
        {
            var section = configuration.GetSection(RedisCacheOptions.SectionName);
            var options = section.Get<RedisCacheOptions>() ?? new RedisCacheOptions();
            services.Configure<RedisCacheOptions>(section);
            services.AddStackExchangeRedisCache(redis =>
            {
                redis.Configuration = options.ConnectionString;
                redis.InstanceName = options.InstanceName;
            });
            services.AddScoped<ICacheService, RedisCacheService>();
            return services;
        }
    }
}
