namespace GAME.Application.DTOs;

public sealed class BattleLabRunDto
{
    public List<BattleLabHeroRunDto> Heroes { get; set; } = [];
    public int Seed { get; set; }
    public string Winner { get; set; } = "";
    public int Rounds { get; set; }
}
