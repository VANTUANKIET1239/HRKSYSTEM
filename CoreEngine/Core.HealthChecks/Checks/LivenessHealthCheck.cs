using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Core.HealthChecks.Checks;

internal sealed class LivenessHealthCheck : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default) =>
        Task.FromResult(HealthCheckResult.Healthy());
}
