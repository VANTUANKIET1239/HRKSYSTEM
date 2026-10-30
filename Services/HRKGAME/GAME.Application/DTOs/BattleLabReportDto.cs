namespace GAME.Application.DTOs;

public sealed class BattleLabReportDto
{
    public int ReportVersion { get; set; } = 2;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public string EngineVersion { get; set; } = "";
    public string SnapshotHash { get; set; } = "";
    public BattleInitialStateDto ResolvedSnapshot { get; set; } = new();
    public List<BattleLabResolvedBuildDto> ResolvedBuilds { get; set; } = [];
    public List<BattleLabSkillMetricDto> Skills { get; set; } = [];
    public List<string> Warnings { get; set; } = [];
    public Dictionary<string, decimal> BattleConfigs { get; set; } = [];
    public BattleLabRequestDto Settings { get; set; } = new();
    public List<BattleLabRunDto> Runs { get; set; } = [];
    public List<BattleHeroStatisticsDto> Totals { get; set; } = [];
    public List<StartBattleResultDto> Replays { get; set; } = [];
}
