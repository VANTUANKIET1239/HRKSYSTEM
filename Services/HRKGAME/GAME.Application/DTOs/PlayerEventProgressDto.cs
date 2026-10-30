namespace GAME.Application.DTOs;

public class PlayerEventProgressDto : PlayerEventProgressSummaryDto
{
    public int EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public int MaxFloor { get; set; }
    public long SecondsUntilReset { get; set; }
    public int PlayerPower { get; set; }
    public PlayerHeroDto? LeadHero { get; set; }
    public List<TowerFloorDto> Floors { get; set; } = new();
    public DateTime PeriodStartUtc { get; set; }
    public DateTime PeriodEndUtc { get; set; }
    public List<TowerChestDto> MilestoneChests { get; set; } = new();
    public List<PlayerPendingRewardDto> PendingRewards { get; set; } = new();
    public bool HasActiveQuickClimb { get; set; }
    public string? ActiveQuickClimbJobId { get; set; }
}
