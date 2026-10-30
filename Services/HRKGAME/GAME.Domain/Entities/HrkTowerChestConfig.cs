namespace GAME.Domain.Entities;

public class HrkTowerChestConfig
{
    public int Id { get; set; }
    public int EventId { get; set; }
    public int FloorNumber { get; set; } // 15, 30, 45, 60
    public string ChestName { get; set; } = null!;
    public string? ChestIcon { get; set; }
    public string? Description { get; set; }
    public long GoldReward { get; set; }
    public int DiamondReward { get; set; }
    public int? GuaranteedItemTemplateId { get; set; }
    public int GuaranteedItemCount { get; set; } = 1;
    public string? RewardsJson { get; set; }
    public bool IsActive { get; set; } = true;

    public virtual HrkGameEvent Event { get; set; } = null!;
    public virtual HrkItemTemplate? GuaranteedItemTemplate { get; set; }
}
