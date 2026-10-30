namespace GAME.Domain.Entities;

public class HrkPlayerPendingReward
{
    public long Id { get; set; }
    public long PlayerId { get; set; }
    public long EventPeriodId { get; set; }
    public string SourceType { get; set; } = "UNCLAIMED_MILESTONE_CHEST"; // UNCLAIMED_MILESTONE_CHEST, BAG_FULL_REWARD
    public int SourceRefId { get; set; } // FloorNumber or ChestId
    public string Description { get; set; } = null!;
    public string RewardItemsJson { get; set; } = null!;
    public string Status { get; set; } = "PENDING"; // PENDING, CLAIMED, EXPIRED
    public DateTime CreatedOnUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ClaimedOnUtc { get; set; }

    public virtual HrkPlayer Player { get; set; } = null!;
    public virtual HrkEventPeriod EventPeriod { get; set; } = null!;
}
