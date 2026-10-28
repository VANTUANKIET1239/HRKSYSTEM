namespace Admin.Application.Configuration;

public sealed class MonitoredService
{
    // Key of InternalApi:Services, which owns the base address.
    public string ServiceName { get; set; } = "";
    public string ExpectedService { get; set; } = "";
    public bool Enabled { get; set; } = true;
}
