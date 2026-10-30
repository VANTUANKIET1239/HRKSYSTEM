namespace GAME.Application.DTOs;

public sealed class StartDungeonStageResultDto
{
    public StartBattleResultDto Battle { get; set; } = null!;
    public DungeonResultDto Result { get; set; } = null!;
}
