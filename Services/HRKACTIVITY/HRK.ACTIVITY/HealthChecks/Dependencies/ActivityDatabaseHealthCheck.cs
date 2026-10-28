using Core.HealthChecks;
using Core.Persistence.Repositories;

namespace HRK.ACTIVITY.HealthChecks.Dependencies;

public sealed class ActivityDatabaseHealthCheck(IUnitOfWork unitOfWork) : DependencyHealthCheck("DatabaseUnavailable")
{
    protected override Task<bool> IsAvailableAsync(CancellationToken cancellationToken) =>
        unitOfWork.CanConnectAsync(cancellationToken);
}
