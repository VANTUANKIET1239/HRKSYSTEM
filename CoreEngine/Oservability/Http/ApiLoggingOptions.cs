namespace Oservability.Http;

public sealed class ApiLoggingOptions
{
    public const string SectionName = "Observability:ApiLogging";
    public bool Enabled { get; set; } = true;
    public bool LogRequestBody { get; set; }
    public bool LogResponseBody { get; set; }
    public int MaxBodyBytes { get; set; } = 4096;
    // Body logging requires both an allowed path and allowed JSON field names.
    // Paths are exact, or end in /* to allow a segment prefix.
    public string[] BodyPaths { get; set; } = [];
    public string[] AllowedBodyFields { get; set; } = [];
    public string[] ExcludedPaths { get; set; } = ["/health", "/swagger"];
}
