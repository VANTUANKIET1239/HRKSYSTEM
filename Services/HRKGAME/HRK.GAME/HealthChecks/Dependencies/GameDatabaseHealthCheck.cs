using Core.HealthChecks;
using Core.Persistence.Repositories;

namespace HRK.GAME.HealthChecks.Dependencies;

public sealed class GameDatabaseHealthCheck(IUnitOfWork unitOfWork) : DependencyHealthCheck("DatabaseUnavailable")
{
    protected override Task<bool> IsAvailableAsync(CancellationToken cancellationToken) =>
        unitOfWork.CanConnectAsync(cancellationToken);
}
