namespace Core.HealthChecks;

public sealed class HrkHealthCheckOptions
{
    public const string SectionName = "HealthChecks";
    public double DefaultTimeoutSeconds { get; set; } = 2;
    public string? InstanceId { get; set; }
    public Dictionary<string, DependencyCheckOptions> Dependencies { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
