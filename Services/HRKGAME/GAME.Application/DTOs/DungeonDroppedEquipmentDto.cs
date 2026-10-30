namespace GAME.Application.DTOs;

public sealed class DungeonDroppedEquipmentDto
{
    public long InventoryItemId { get; set; }
    public int ItemTemplateId { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? ImagePath { get; set; }
    public string RarityCode { get; set; } = null!;
    public string RarityName { get; set; } = null!;
    public string? RarityColorHex { get; set; }
    public string CategoryCode { get; set; } = null!;
    public string? CategoryName { get; set; }
    public int Count { get; set; } = 1;
    public decimal EnhancementGrowthPercent { get; set; }
    public decimal EnhancementGrowthMinPercent { get; set; }
    public decimal EnhancementGrowthMaxPercent { get; set; }
    public decimal OverallRollPercent { get; set; }
    public List<EquipmentRolledAttributeDto> RolledAttributes { get; set; } = new();
    public Dictionary<string, decimal> CurrentStats { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public int CombatPower { get; set; }
}
