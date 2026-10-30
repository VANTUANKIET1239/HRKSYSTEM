namespace GAME.Application.DTOs;

public sealed class DungeonStarChestDto
{
    public int Id { get; set; }
    public int DungeonMapId { get; set; }
    public int RequiredStars { get; set; }
    public string State { get; set; } = "LOCKED"; // LOCKED, CLAIMABLE, CLAIMED
    public long GoldReward { get; set; }
    public int DiamondReward { get; set; }
    public int UpgradeMaterialsReward { get; set; }
    public DungeonPossibleDropDto? GuaranteedItem { get; set; }
    public int DisplayOrder { get; set; }
    public string? Description { get; set; }
    public DateTime? ClaimedOn { get; set; }
}
