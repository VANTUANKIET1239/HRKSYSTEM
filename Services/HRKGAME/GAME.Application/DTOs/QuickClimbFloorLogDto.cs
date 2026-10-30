namespace GAME.Application.DTOs;

public class QuickClimbFloorLogDto
{
    public int FloorNumber { get; set; }
    public string FloorName { get; set; } = null!;
    public bool IsVictory { get; set; }
    public int TotalTurns { get; set; }
    public int LivesRemaining { get; set; }
    public List<GenericRewardItemDto> RewardsGained { get; set; } = new();
    public DateTime TimestampUtc { get; set; }
}
