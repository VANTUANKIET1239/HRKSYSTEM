namespace GAME.Application.DTOs;

public sealed class StartDungeonStageRequestDto
{
    public string FormationCode { get; set; } = "DEFAULT";
    public List<FormationPositionRequestDto> Positions { get; set; } = new();
    public string ClientRequestId { get; set; } = Guid.NewGuid().ToString("N");
}
