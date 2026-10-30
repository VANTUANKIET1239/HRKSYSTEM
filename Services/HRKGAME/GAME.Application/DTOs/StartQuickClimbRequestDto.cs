namespace GAME.Application.DTOs;

public class StartQuickClimbRequestDto
{
    public string? FormationCode { get; set; }
    public List<FormationPositionRequestDto>? Positions { get; set; }
}
