using Core.Persistence.Repositories;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace HRK.GAME.Health;

public sealed class GameDatabaseHealthCheck(IUnitOfWork unitOfWork) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await unitOfWork.CanConnectAsync(cancellationToken)
                ? HealthCheckResult.Healthy("GAME database is reachable.")
                : HealthCheckResult.Unhealthy("GAME database is unreachable.");
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("GAME database is unavailable.", exception);
        }
    }
}
