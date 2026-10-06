using Core.RabbitMQ.Interfaces;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace HRK.REALTIME.Health;

public sealed class RabbitMqHealthCheck(IRabbitMqConnection connection) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var current = await connection.GetConnectionAsync(cancellationToken);
            return current.IsOpen
                ? HealthCheckResult.Healthy("RabbitMQ connection is open.")
                : HealthCheckResult.Unhealthy("RabbitMQ connection is closed.");
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("RabbitMQ is unavailable.", exception);
        }
    }
}
