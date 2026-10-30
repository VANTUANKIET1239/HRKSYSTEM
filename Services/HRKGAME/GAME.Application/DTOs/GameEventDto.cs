namespace GAME.Application.DTOs;

public class GameEventDto
{
    public int Id { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string EventType { get; set; } = "TOWER";
    public string? Description { get; set; }
    public string BannerImagePath { get; set; } = null!;
    public string? Icon { get; set; }
    public int DisplayOrder { get; set; }
    public int MinPlayerLevel { get; set; } = 1;
    public bool IsOpen { get; set; }
    public string Status { get; set; } = "OPEN"; // OPEN, LOCKED, ENDED
    public string? LockReason { get; set; }
    public long RemainingSecondsToReset { get; set; }
    public DateTime NextResetTimeUtc { get; set; }
    public int InitialLives { get; set; } = 3;
    public int MaxFloor { get; set; } = 60;
    public PlayerEventProgressSummaryDto? PlayerProgress { get; set; }
    public List<GenericRewardItemDto> HighlightRewards { get; set; } = new();
}
