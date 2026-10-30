using Core.InternalApi;
using Core.Caching;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Core.Localization;

public static class LocalizationRegistration
{
    public static IServiceCollection AddHrkLocalization(this IServiceCollection services, IConfiguration configuration,
        string group = "common")
    {
        services.AddOptions<LocalizationOptions>().Configure(options =>
        {
            options.Group = group;
            configuration.GetSection("Localization").Bind(options);
            options.RedisConnectionString = configuration["Localization:RedisConnectionString"]
                ?? configuration["Redis:ConnectionString"] ?? options.RedisConnectionString;
            options.DefaultCulture = TranslationIdentity.Culture(options.DefaultCulture) ?? options.DefaultCulture;
        }).Validate(options => TranslationIdentity.ValidGroup(options.Group) &&
            TranslationIdentity.Culture(options.DefaultCulture) is not null && options.CacheSeconds > 0 &&
            options.ApiTimeoutSeconds > 0 && !string.IsNullOrWhiteSpace(options.RedisKeyPrefix),
            "Invalid Localization configuration.").ValidateOnStart();
        services.AddHrkRedisConnections();
        services.TryAddSingleton<ITranslationCache, RedisTranslationCache>();
        services.TryAddScoped<ITextLocalizer, TextLocalizer>();
        return services;
    }
}
