using GAME.Infrastructure.Data;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;

namespace HRK.GAME.Health;

public sealed class GameDatabaseHealthCheck(GameDbContext db) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await db.Database.CanConnectAsync(cancellationToken)
                ? HealthCheckResult.Healthy("GAME database is reachable.")
                : HealthCheckResult.Unhealthy("GAME database is unreachable.");
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("GAME database is unavailable.", exception);
        }
    }
}
