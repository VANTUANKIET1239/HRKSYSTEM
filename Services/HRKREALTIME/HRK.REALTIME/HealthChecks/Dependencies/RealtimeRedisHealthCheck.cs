using Core.Caching.Redis;
using Core.HealthChecks;

namespace HRK.REALTIME.HealthChecks.Dependencies;

public sealed class RealtimeRedisHealthCheck(IRedisConnectionProvider connections, IConfiguration configuration)
    : DependencyHealthCheck("RedisUnavailable")
{
    protected override async Task<bool> IsAvailableAsync(CancellationToken cancellationToken)
    {
        var database = await connections.GetDatabaseAsync(
            configuration["Redis:ConnectionString"] ?? "localhost:6379,abortConnect=false", cancellationToken);
        await database.PingAsync().WaitAsync(cancellationToken);
        return true;
    }
}
