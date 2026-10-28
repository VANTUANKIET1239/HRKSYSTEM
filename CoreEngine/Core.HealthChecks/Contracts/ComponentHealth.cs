namespace Core.HealthChecks;

public sealed record ComponentHealth
{
    public string Name { get; init; } = "";
    public string Status { get; init; } = "";
    public double DurationMs { get; init; }
    public string? ErrorCode { get; init; }
}
