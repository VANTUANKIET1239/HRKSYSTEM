namespace GAME.Domain.Entities;

public class HrkPlayerEventPeriodProgress
{
    public long Id { get; set; }
    public long PlayerId { get; set; }
    public int EventId { get; set; }
    public long EventPeriodId { get; set; }
    public string PeriodKey { get; set; } = null!;
    public int CurrentFloor { get; set; } = 1;
    public int RemainingLives { get; set; } = 3;
    public int CurrentRunNumber { get; set; } = 1;
    public int QuickClimbRunsUsed { get; set; }
    public int HighestFloorInPeriod { get; set; } = 0;
    public bool IsCompleted { get; set; } = false;
    public DateTime? ClosedAtUtc { get; set; }
    public Guid? ResetExecutionId { get; set; }
    public string? ResetSummaryJson { get; set; }
    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedOn { get; set; } = DateTime.UtcNow;

    public virtual HrkPlayer Player { get; set; } = null!;
    public virtual HrkGameEvent Event { get; set; } = null!;
    public virtual HrkEventPeriod EventPeriod { get; set; } = null!;
}
