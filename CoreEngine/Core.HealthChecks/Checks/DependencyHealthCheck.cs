using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Core.HealthChecks;

/// <summary>Optional base for service-owned checks. No vendor dependency or connection ownership.</summary>
public abstract class DependencyHealthCheck(string failureCode) : IHealthCheck
{
    protected abstract Task<bool> IsAvailableAsync(CancellationToken cancellationToken);

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await IsAvailableAsync(cancellationToken)
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Unhealthy(data: new Dictionary<string, object> { ["errorCode"] = failureCode });
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy(exception: exception,
                data: new Dictionary<string, object> { ["errorCode"] = failureCode });
        }
    }
}
