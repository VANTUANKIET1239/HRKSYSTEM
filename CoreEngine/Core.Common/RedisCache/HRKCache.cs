

namespace CoreEngine.RedisCache
{


    public interface IHRKCache
    {
        Task SetCache(string key, string value, CancellationToken cancellationToken);
        Task<string> GetCache(string key, CancellationToken cancellationToken);
        Task RemoveAsync(string key);
    }
    public class HRKCache  : IHRKCache
    {
        private readonly IDistributedCache distributedCache;

        public HRKCache(IDistributedCache distributedCache)
        {
            this.distributedCache = distributedCache;
        }


        public async Task SetCache(string key, string value, CancellationToken cancellationToken)
        {
            var options = new DistributedCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromMinutes(5))
                .SetAbsoluteExpiration(DateTimeOffset.UtcNow.AddHours(1));
            await distributedCache.SetStringAsync(key, value, cancellationToken);
        }

        public async Task<string> GetCache(string key, CancellationToken cancellationToken)
        {
            //var options = new DistributedCacheEntryOptions()
            //    .SetSlidingExpiration(TimeSpan.FromMinutes(5))
            //    .SetAbsoluteExpiration(DateTimeOffset.UtcNow.AddHours(1));
          return await distributedCache.GetStringAsync(key, cancellationToken) ?? "";
        }

        public async Task RemoveAsync(string key)
        {
            await distributedCache.RemoveAsync(key);
        }
    }
}
