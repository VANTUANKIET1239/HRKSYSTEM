namespace ApiGateway.Configuration;

public sealed class DownstreamHealthCheckOptions
{
    public string ServiceName { get; set; } = "";
    public string ExpectedService { get; set; } = "";
    public string Path { get; set; } = "/health/live";
}
