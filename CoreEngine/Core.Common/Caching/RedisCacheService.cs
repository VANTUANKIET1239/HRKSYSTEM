using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace Core.Common.Caching
{
    /// <summary>
    /// JSON cache over IDistributedCache. Redis errors deliberately fail open so a
    /// temporary cache outage never makes a business request unavailable.
    /// </summary>
    internal sealed class RedisCacheService : ICacheService
    {
        private readonly IDistributedCache _cache;
        private readonly ILogger<RedisCacheService> _logger;
        private readonly RedisCacheOptions _options;
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        public RedisCacheService(IDistributedCache cache, IOptions<RedisCacheOptions> options, ILogger<RedisCacheService> logger)
        {
            _cache = cache;
            _logger = logger;
            _options = options.Value;
        }

        public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
        {
            try
            {
                var json = await _cache.GetStringAsync(key, cancellationToken);
                return string.IsNullOrWhiteSpace(json) ? default : JsonSerializer.Deserialize<T>(json, JsonOptions);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Redis GET failed for key {CacheKey}; continuing without cache.", key);
                return default;
            }
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken cancellationToken = default)
        {
            try
            {
                var ttl = expiry ?? TimeSpan.FromMinutes(_options.DefaultExpirationMinutes);
                await _cache.SetStringAsync(key, JsonSerializer.Serialize(value, JsonOptions), new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = ttl
                }, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Redis SET failed for key {CacheKey}; continuing without cache.", key);
            }
        }

        public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            try { await _cache.RemoveAsync(key, cancellationToken); }
            catch (Exception ex) { _logger.LogWarning(ex, "Redis REMOVE failed for key {CacheKey}.", key); }
        }
    }
}
