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
