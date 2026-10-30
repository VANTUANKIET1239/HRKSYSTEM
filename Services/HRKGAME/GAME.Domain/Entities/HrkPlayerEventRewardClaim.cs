namespace GAME.Domain.Entities;

public class HrkPlayerEventRewardClaim
{
    public long Id { get; set; }
    public long PlayerId { get; set; }
    public long EventPeriodId { get; set; }
    public string ClaimType { get; set; } = null!; // FLOOR_REWARD, MILESTONE_CHEST, PENDING_REWARD
    public int TargetId { get; set; } // FloorNumber or ChestId
    public DateTime ClaimedAtUtc { get; set; } = DateTime.UtcNow;
    public string? RewardSummaryJson { get; set; }

    public virtual HrkPlayer Player { get; set; } = null!;
    public virtual HrkEventPeriod EventPeriod { get; set; } = null!;
}
