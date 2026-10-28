using Core.HealthChecks;

namespace Admin.Application.DTOs.HealthMonitoring;

public sealed record EndpointHealth
{
    public string Status { get; init; } = HealthStatuses.Unhealthy;
    public int? HttpStatusCode { get; init; }
    public double DurationMs { get; init; }
    public string? ErrorCode { get; init; }
    public ServiceHealthReport? Report { get; init; }
}
