namespace Admin.Application.DTOs.HealthMonitoring;

public sealed record MonitoredServiceHealth
{
    public string Target { get; init; } = "";
    public string Service { get; init; } = "";
    public string Status { get; init; } = "";
    public EndpointHealth Liveness { get; init; } = new();
    // Null when liveness could not return a valid report; dependency state is then unknown.
    public EndpointHealth? Dependencies { get; init; }
}
