namespace GAME.Application.DTOs;

public sealed class BattleLabSlotDto
{
    public int HeroTemplateId { get; set; }
    public int Position { get; set; }
    public string Mode { get; set; } = "CUSTOM";
    public CalculatedStatsDto? Stats { get; set; }
    public BattleLabBuildDto? Build { get; set; }
}
