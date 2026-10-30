namespace GAME.Application.DTOs;

public class DungeonMapDto
{
    public int Id { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string ImagePath { get; set; } = null!;
    public string BackgroundPath { get; set; } = null!;
    public int RequiredPlayerLevel { get; set; }
    public int ClearedStages { get; set; }
    public int TotalStages { get; set; }
    public string State { get; set; } = null!;
}
