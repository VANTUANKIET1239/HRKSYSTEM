namespace GAME.Application.DTOs;

public class StartTowerBattleRequestDto
{
    public string ClientRequestId { get; set; } = null!;
    public string? FormationCode { get; set; }
    public List<FormationPositionRequestDto>? Positions { get; set; }
}
