namespace ApiGateway.Configuration;

public sealed class GatewayHealthCheckOptions
{
    public const string SectionName = "GatewayHealthChecks";
    public Dictionary<string, DownstreamHealthCheckOptions> Dependencies { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
