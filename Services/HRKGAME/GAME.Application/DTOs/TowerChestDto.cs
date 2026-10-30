namespace GAME.Application.DTOs;

public class TowerChestDto
{
    public int Id { get; set; }
    public int FloorNumber { get; set; }
    public string ChestName { get; set; } = null!;
    public string? ChestIcon { get; set; }
    public string? Description { get; set; }
    public string State { get; set; } = "LOCKED"; // LOCKED, UNLOCKED, CLAIMED
    public List<GenericRewardItemDto> Rewards { get; set; } = new();
}
