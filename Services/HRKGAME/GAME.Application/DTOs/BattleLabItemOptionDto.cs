namespace GAME.Application.DTOs;

public sealed class BattleLabItemOptionDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Category { get; set; } = "";
    public int LevelReq { get; set; }
}
