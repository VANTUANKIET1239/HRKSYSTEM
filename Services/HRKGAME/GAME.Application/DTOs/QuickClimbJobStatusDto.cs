namespace GAME.Application.DTOs;

public class QuickClimbJobStatusDto
{
    public string JobId { get; set; } = null!;
    public string Status { get; set; } = "QUEUED"; // QUEUED, PROCESSING, COMPLETED, STOPPED_DEFEAT, CANCELLED, EXPIRED, ERROR
    public string? StopReason { get; set; } // FIRST_DEFEAT, TOWER_COMPLETED, USER_CANCELLED, PERIOD_EXPIRED, EVENT_CLOSED, RULES_CHANGED, ERROR
    public int StartFloor { get; set; }
    public int CurrentFloor { get; set; }
    public int TargetFloor { get; set; }
    public int InitialLives { get; set; }
    public int RemainingLives { get; set; }
    public int ClearedFloorsCount { get; set; }
    public int DailyRunNumber { get; set; }
    public int? FailedFloor { get; set; }
    public List<GenericRewardItemDto> AccumulatedRewards { get; set; } = new();
    public List<QuickClimbFloorLogDto> Logs { get; set; } = new();
    public string? LastBattleId { get; set; }
    public List<TowerChestDto> UnlockedChests { get; set; } = new();
    public DateTime CreatedOnUtc { get; set; }
    public DateTime UpdatedOnUtc { get; set; }
    public DateTime? CompletedOnUtc { get; set; }
    public long Version { get; set; }
}
