namespace GAME.Application.DTOs;

public sealed class BattleLabHeroRunDto
{
    public BattleHeroStatisticsDto Statistics { get; set; } = new();
    public int RemainingHp { get; set; }
    public int SkillCasts { get; set; }
    public int EnergyCasts { get; set; }
}
