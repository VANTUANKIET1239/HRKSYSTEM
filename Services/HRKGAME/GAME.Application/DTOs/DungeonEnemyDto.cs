namespace GAME.Application.DTOs;

public sealed class DungeonEnemyDto
{
    public int Position { get; set; }
    public string Name { get; set; } = null!;
    public string ImagePath { get; set; } = null!;
    public int Level { get; set; }
    public byte Stars { get; set; }
    public bool IsBoss { get; set; }
    public int Power { get; set; }
}
