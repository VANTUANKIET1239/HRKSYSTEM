namespace Core.HealthChecks;

public static class HealthStatuses
{
    public const string Healthy = "Healthy";
    public const string Unhealthy = "Unhealthy";
    public static bool IsValid(string? value) => value is Healthy or Unhealthy;
}
