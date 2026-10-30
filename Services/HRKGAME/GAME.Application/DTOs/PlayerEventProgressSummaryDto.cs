namespace GAME.Application.DTOs;

public class PlayerEventProgressSummaryDto
{
    public int CurrentFloor { get; set; } = 1;
    public int RemainingLives { get; set; } = 3;
    public int InitialLives { get; set; } = 3;
    public int CurrentRunNumber { get; set; } = 1;
    public int QuickClimbRunsUsed { get; set; }
    public int QuickClimbDailyLimit { get; set; } = 3;
    public int QuickClimbRunsRemaining { get; set; } = 3;
    public bool CanStartQuickClimb { get; set; } = true;
    public int HighestFloorInPeriod { get; set; } = 0;
    public int HighestFloorAllTime { get; set; } = 0;
    public bool IsCompleted { get; set; }
    public bool CanStartNewRun { get; set; }
    public string PeriodKey { get; set; } = null!;
    public int PendingRewardsCount { get; set; }
}
