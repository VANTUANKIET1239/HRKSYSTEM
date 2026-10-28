using System.Net;
using ApiGateway.Configuration;
using Core.HealthChecks;
using Core.InternalApi;

namespace ApiGateway.HealthChecks.Dependencies;

// Only probe downstream liveness here. Probing the Admin aggregate would create a cycle.
public sealed class DownstreamServiceHealthCheck(IInternalApiClient client, DownstreamHealthCheckOptions target)
    : DependencyHealthCheck("DownstreamUnavailable")
{
    protected override async Task<bool> IsAvailableAsync(CancellationToken cancellationToken)
    {
        var response = await client.GetResponseAsync<ServiceHealthReport>(target.ServiceName, target.Path,
            [HttpStatusCode.OK, HttpStatusCode.ServiceUnavailable],
            cancellationToken: cancellationToken);
        return response.StatusCode == HttpStatusCode.OK &&
            response.Body.IsValid(target.ExpectedService, HealthKinds.Liveness) &&
            response.Body.Status == HealthStatuses.Healthy;
    }
}
