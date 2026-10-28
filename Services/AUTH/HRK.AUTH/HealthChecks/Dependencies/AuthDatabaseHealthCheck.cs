using Core.HealthChecks;
using Core.Persistence.Repositories;

namespace HRK.AUTH.HealthChecks.Dependencies;

public sealed class AuthDatabaseHealthCheck(IUnitOfWork unitOfWork) : DependencyHealthCheck("DatabaseUnavailable")
{
    protected override Task<bool> IsAvailableAsync(CancellationToken cancellationToken) =>
        unitOfWork.CanConnectAsync(cancellationToken);
}
