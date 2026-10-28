using System.Diagnostics;
using System.Net;
using Admin.Application.Configuration;
using Admin.Application.DTOs.HealthMonitoring;
using Admin.Application.Interfaces.HealthMonitoring;
using Core.HealthChecks;
using Core.InternalApi;
using Microsoft.Extensions.Options;

namespace Admin.Infrastructure.HealthMonitoring;

public sealed class InternalApiHealthProbe(IInternalApiClient client, IOptions<HealthMonitoringOptions> options)
    : IServiceHealthProbe
{
    public Task<EndpointHealth> CheckLivenessAsync(MonitoredService target, CancellationToken cancellationToken) =>
        ProbeAsync(target, "/health/live", HealthKinds.Liveness, cancellationToken);

    public Task<EndpointHealth> CheckDependenciesAsync(MonitoredService target, CancellationToken cancellationToken) =>
        ProbeAsync(target, "/health/dependencies", HealthKinds.Dependency, cancellationToken);

    private async Task<EndpointHealth> ProbeAsync(MonitoredService target, string path, string kind,
        CancellationToken cancellationToken)
    {
        var started = Stopwatch.GetTimestamp();
        int? statusCode = null;
        try
        {
            var response = await client.GetResponseAsync<ServiceHealthReport>(target.ServiceName, path,
                [HttpStatusCode.OK, HttpStatusCode.ServiceUnavailable],
                new() { RetryEnabled = false, TimeoutSeconds = options.Value.ProbeTimeoutSeconds }, cancellationToken);
            statusCode = (int)response.StatusCode;
            var report = response.Body;
            if (report is null || !report.IsValid(target.ExpectedService, kind) ||
                (response.StatusCode == HttpStatusCode.OK) != (report.Status == HealthStatuses.Healthy))
                return Failed("InvalidResponse");
            return new()
            {
                Status = report.Status, HttpStatusCode = statusCode, Report = report,
                DurationMs = Stopwatch.GetElapsedTime(started).TotalMilliseconds
            };
        }
        catch (InternalApiException error) { statusCode = (int?)error.HttpStatusCode; return Failed(error.Failure.ToString()); }
        catch (OperationCanceledException) { throw; }
        catch (Exception) { return Failed("CheckFailed"); }

        EndpointHealth Failed(string code) => new()
        {
            HttpStatusCode = statusCode, ErrorCode = code,
            DurationMs = Stopwatch.GetElapsedTime(started).TotalMilliseconds
        };
    }
}
