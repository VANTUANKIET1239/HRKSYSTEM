namespace Core.HealthChecks;

public sealed record ServiceHealthReport
{
    public int ContractVersion { get; init; } = 1;
    public string Service { get; init; } = "";
    public string InstanceId { get; init; } = "";
    public string Kind { get; init; } = "";
    public string Status { get; init; } = "";
    public DateTimeOffset CheckedAtUtc { get; init; }
    public double DurationMs { get; init; }
    public IReadOnlyList<ComponentHealth> Checks { get; init; } = [];

    public bool IsValid(string expectedService, string expectedKind) =>
        ContractVersion == 1 && Service == expectedService && Kind == expectedKind &&
        !string.IsNullOrWhiteSpace(InstanceId) && InstanceId.Length <= 128 &&
        HealthStatuses.IsValid(Status) && CheckedAtUtc != default && ValidDuration(DurationMs) &&
        Checks is { Count: <= 128 } && (Kind != HealthKinds.Liveness || Checks.Count > 0) &&
        Checks.All(check => check is not null && !string.IsNullOrWhiteSpace(check.Name) && check.Name.Length <= 128 &&
            HealthStatuses.IsValid(check.Status) && ValidDuration(check.DurationMs)) &&
        Checks.Select(check => check.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count() == Checks.Count &&
        Status == (Checks.Any(check => check.Status == HealthStatuses.Unhealthy) ? HealthStatuses.Unhealthy : HealthStatuses.Healthy);

    private static bool ValidDuration(double value) => double.IsFinite(value) && value >= 0;
}
