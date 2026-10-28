using Core.HealthChecks;
using Core.Persistence.Repositories;

namespace HRK.LANGUAGE.HealthChecks.Dependencies;

public sealed class LanguageDatabaseHealthCheck(IUnitOfWork unitOfWork) : DependencyHealthCheck("DatabaseUnavailable")
{
    protected override Task<bool> IsAvailableAsync(CancellationToken cancellationToken) =>
        unitOfWork.CanConnectAsync(cancellationToken);
}
