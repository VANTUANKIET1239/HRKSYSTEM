using Admin.Application.Configuration;
using Admin.Application.DTOs.HealthMonitoring;

namespace Admin.Application.Interfaces.HealthMonitoring;

public interface IServiceHealthProbe
{
    Task<EndpointHealth> CheckLivenessAsync(MonitoredService target, CancellationToken cancellationToken);
    Task<EndpointHealth> CheckDependenciesAsync(MonitoredService target, CancellationToken cancellationToken);
}
