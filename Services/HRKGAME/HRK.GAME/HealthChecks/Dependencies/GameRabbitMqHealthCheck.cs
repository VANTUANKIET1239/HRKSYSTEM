using Core.HealthChecks;
using Core.RabbitMQ.Interfaces;

namespace HRK.GAME.HealthChecks.Dependencies;

public sealed class GameRabbitMqHealthCheck(IRabbitMqConnection connection) : DependencyHealthCheck("RabbitMqUnavailable")
{
    protected override async Task<bool> IsAvailableAsync(CancellationToken cancellationToken) =>
        (await connection.GetConnectionAsync(cancellationToken)).IsOpen;
}
