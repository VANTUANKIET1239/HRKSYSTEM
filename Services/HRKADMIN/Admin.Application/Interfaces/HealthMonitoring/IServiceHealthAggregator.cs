using Admin.Application.DTOs.HealthMonitoring;

namespace Admin.Application.Interfaces.HealthMonitoring;

public interface IServiceHealthAggregator
{
    Task<SystemHealthReport> CheckAsync(CancellationToken cancellationToken = default);
}
