using Core.HealthChecks;
using Core.RabbitMQ.Interfaces;

namespace HRK.REALTIME.HealthChecks.Dependencies;

public sealed class RealtimeRabbitMqHealthCheck(IRabbitMqConnection connection) : DependencyHealthCheck("RabbitMqUnavailable")
{
    protected override async Task<bool> IsAvailableAsync(CancellationToken cancellationToken) =>
        (await connection.GetConnectionAsync(cancellationToken)).IsOpen;
}
