namespace GAME.Application.DTOs;

public class GenericRewardItemDto
{
    public string Type { get; set; } = "GOLD"; // GOLD, DIAMOND, ENHANCEMENT_STONE, CHARM, HERO_STONE, UNIVERSAL_STAR_STONE, MATERIAL, EQUIPMENT, PLAYER_EXP, HERO_EXP
    public int? ItemTemplateId { get; set; }
    public string? Code { get; set; }
    public string Name { get; set; } = null!;
    public string? ImagePath { get; set; }
    public string? RarityCode { get; set; }
    public string? RarityColorHex { get; set; }
    public string? CategoryName { get; set; }
    public int Quantity { get; set; } = 1;
    public int? StoneGrade { get; set; }
    public string? CharmType { get; set; }
    public string? Description { get; set; }
    public DungeonDroppedEquipmentDto? DroppedEquipment { get; set; }
}
