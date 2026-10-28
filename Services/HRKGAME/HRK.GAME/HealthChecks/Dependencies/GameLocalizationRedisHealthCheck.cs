using Core.Caching.Redis;
using Core.HealthChecks;
using Core.Localization;
using Microsoft.Extensions.Options;

namespace HRK.GAME.HealthChecks.Dependencies;

public sealed class GameLocalizationRedisHealthCheck(IRedisConnectionProvider connections, IOptions<LocalizationOptions> options)
    : DependencyHealthCheck("RedisUnavailable")
{
    protected override async Task<bool> IsAvailableAsync(CancellationToken cancellationToken)
    {
        var database = await connections.GetDatabaseAsync(options.Value.RedisConnectionString, cancellationToken);
        await database.PingAsync().WaitAsync(cancellationToken);
        return true;
    }
}
