namespace Admin.Application.DTOs.HealthMonitoring;

public sealed record SystemHealthReport
{
    public string Status { get; init; } = "";
    public DateTimeOffset CheckedAtUtc { get; init; }
    public double DurationMs { get; init; }
    public IReadOnlyList<MonitoredServiceHealth> Services { get; init; } = [];
}
