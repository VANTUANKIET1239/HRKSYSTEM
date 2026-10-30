namespace GAME.Application.DTOs;

public class ClaimRewardResultDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = null!;
    public List<GenericRewardItemDto> ClaimedRewards { get; set; } = new();
    public bool IsBagFull { get; set; }
}
