namespace Core.HealthChecks;

public sealed class DependencyCheckOptions
{
    public bool Enabled { get; set; } = true;
    public double? TimeoutSeconds { get; set; }
}
