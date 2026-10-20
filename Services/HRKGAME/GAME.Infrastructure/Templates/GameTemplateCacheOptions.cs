namespace GAME.Infrastructure.Templates;

public sealed class GameTemplateCacheOptions
{
    public const string SectionName = "GameTemplateCache";
    public bool Enabled { get; set; } = true;
    public string DataVersion { get; set; } = "1";
    public bool WarmupOnStartup { get; set; } = true;
    public bool AllowManualRefresh { get; set; }
    public int LocalExpirationMinutes { get; set; } = 30;
    public int DistributedExpirationMinutes { get; set; } = 1440;
    public int OperationTimeoutSeconds { get; set; } = 60;
    public int MaximumTableBytes { get; set; } = 8 * 1024 * 1024 - 1024;
    public string[] Tables { get; set; } = [];
}
