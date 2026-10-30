namespace GAME.Application.DTOs;

public sealed class ClaimStarChestResultDto
{
    public int ChestId { get; set; }
    public int RequiredStars { get; set; }
    public long GoldGained { get; set; }
    public int DiamondsGained { get; set; }
    public int UpgradeMaterialsGained { get; set; }
    public DungeonDroppedEquipmentDto? DroppedEquipment { get; set; }
    public DungeonStarChestDto Chest { get; set; } = null!;
}
