using StackExchange.Redis;

namespace Core.Caching.Redis;

/// <summary>Reuses an owned Redis connection for each connection configuration.</summary>
public interface IRedisConnectionProvider
{
    Task<IDatabase> GetDatabaseAsync(string configuration, CancellationToken cancellationToken = default);
}
