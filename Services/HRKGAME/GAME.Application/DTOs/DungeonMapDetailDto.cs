namespace GAME.Application.DTOs;

public sealed class DungeonMapDetailDto : DungeonMapDto
{
    public int TotalStars { get; set; }
    public List<DungeonStarChestDto> StarChests { get; set; } = new();
    public List<DungeonStageDto> Stages { get; set; } = new();
}
