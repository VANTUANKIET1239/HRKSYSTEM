namespace GAME.Application.DTOs;

public class TowerFloorDto
{
    public int FloorNumber { get; set; }
    public string Name { get; set; } = null!;
    public string FloorType { get; set; } = "NORMAL"; // NORMAL, ELITE, BOSS
    public int RecommendedPower { get; set; }
    public string? BackgroundPath { get; set; }
    public string State { get; set; } = "LOCKED"; // CLEARED, CURRENT, LOCKED
    public TowerEnemyDto? RepresentativeEnemy { get; set; }
    public bool HasMilestoneChest { get; set; }
    public TowerChestDto? ChestInfo { get; set; }
    public List<GenericRewardItemDto> Rewards { get; set; } = new();
}
