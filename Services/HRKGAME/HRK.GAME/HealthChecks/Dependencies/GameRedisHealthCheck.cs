using Core.Caching.Redis;
using Core.HealthChecks;

namespace HRK.GAME.HealthChecks.Dependencies;

public sealed class GameRedisHealthCheck(IRedisConnectionProvider connections, IConfiguration configuration)
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
