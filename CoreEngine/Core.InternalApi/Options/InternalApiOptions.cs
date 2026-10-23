namespace Core.InternalApi;

public sealed class InternalApiOptions
{
    public const string SectionName = "InternalApi";
    public Dictionary<string, InternalServiceOptions> Services { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public long MaxBufferedBytes { get; init; } = 10L * 1024 * 1024;
}
