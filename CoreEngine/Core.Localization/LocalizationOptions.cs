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
