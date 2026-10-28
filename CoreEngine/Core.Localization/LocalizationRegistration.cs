using Core.InternalApi;
using Core.Caching;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Core.Localization;

public sealed class LocalizationOptions
{
    public bool Enabled { get; set; } = true;
    public string Group { get; set; } = "common";
    public string DefaultCulture { get; set; } = "vi-VN";
    public string CookieName { get; set; } = "hrk_language";
    public string ServiceName { get; set; } = "language";
    public string RedisConnectionString { get; set; } = "localhost:6379,abortConnect=false,connectTimeout=1000,asyncTimeout=1000";
    // Shared across applications, independent of their business Redis InstanceName.
    public string RedisKeyPrefix { get; set; } = "hrk:dev:i18n:";
    public int CacheSeconds { get; set; } = 3600;
    public double ApiTimeoutSeconds { get; set; } = 2;
}

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
