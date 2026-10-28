namespace Admin.Application.Configuration;

public sealed class HealthMonitoringOptions
{
    public const string SectionName = "HealthMonitoring";
    public double RequestTimeoutSeconds { get; set; } = 10;
    public double ProbeTimeoutSeconds { get; set; } = 4;
    public int MaxConcurrency { get; set; } = 4;
    public List<MonitoredService> Services { get; set; } = [];
}
