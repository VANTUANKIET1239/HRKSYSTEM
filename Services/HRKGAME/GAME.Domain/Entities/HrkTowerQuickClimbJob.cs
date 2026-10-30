namespace GAME.Domain.Entities;

public class HrkTowerQuickClimbJob
{
    public long Id { get; set; }
    public string JobId { get; set; } = null!;
    public long PlayerId { get; set; }
    public long EventPeriodId { get; set; }
    public string Status { get; set; } = "QUEUED"; // QUEUED, PROCESSING, COMPLETED, STOPPED_DEFEAT, CANCELLED, EXPIRED, ERROR
    public string? StopReason { get; set; } // FIRST_DEFEAT, TOWER_COMPLETED, USER_CANCELLED, PERIOD_EXPIRED, EVENT_CLOSED, RULES_CHANGED, ERROR
    public int StartFloor { get; set; }
    public int CurrentFloor { get; set; }
    public int TargetFloor { get; set; }
    public int InitialLives { get; set; }
    public int RemainingLives { get; set; }
    public int ClearedFloorsCount { get; set; } = 0;
    public int DailyRunNumber { get; set; }
    public int? FailedFloor { get; set; }
    public string FormationCode { get; set; } = null!;
    public string FormationSnapshotJson { get; set; } = null!;
    public string? AccumulatedRewardsJson { get; set; }
    public string? LogsJson { get; set; }
    public string? LastBattleId { get; set; }
    public string? WorkerId { get; set; }
    public DateTime? LockedUntilUtc { get; set; }
    public DateTime CreatedOnUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedOnUtc { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedOnUtc { get; set; }
    public long Version { get; set; }

    public virtual HrkPlayer Player { get; set; } = null!;
    public virtual HrkEventPeriod EventPeriod { get; set; } = null!;
}
