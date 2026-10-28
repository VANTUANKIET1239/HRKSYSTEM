namespace ApiGateway.Configuration;

public sealed class GatewayHealthCheckOptions
{
    public const string SectionName = "GatewayHealthChecks";
    public Dictionary<string, DownstreamHealthCheckOptions> Dependencies { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class DownstreamHealthCheckOptions
{
    public string ServiceName { get; set; } = "";
    public string ExpectedService { get; set; } = "";
    public string Path { get; set; } = "/health/live";
}
