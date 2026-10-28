using Core.HealthChecks;
using Core.RabbitMQ.Interfaces;

namespace HRK.ACTIVITY.HealthChecks.Dependencies;

public sealed class ActivityRabbitMqHealthCheck(IRabbitMqConnection connection) : DependencyHealthCheck("RabbitMqUnavailable")
{
    protected override async Task<bool> IsAvailableAsync(CancellationToken cancellationToken) =>
        (await connection.GetConnectionAsync(cancellationToken)).IsOpen;
}
