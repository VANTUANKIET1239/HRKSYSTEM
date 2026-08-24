using System;

namespace GAME.Application.DTOs
{
    public class InventoryItemDto
    {
        public long Id { get; set; }
        public int ItemTemplateId { get; set; }
        public string Name { get; set; } = null!;
        public string Icon { get; set; } = null!;
        public int RarityId { get; set; }
        public string RarityCode { get; set; } = null!;
        public string RarityName { get; set; } = null!;
        public string? RarityColorHex { get; set; }
        public int CategoryId { get; set; }
        public string CategoryCode { get; set; } = null!;
        public string CategoryName { get; set; } = null!;
        public bool IsEquipment { get; set; }
        public int Count { get; set; }
        public int LevelReq { get; set; }
        public string? Description { get; set; }
        public object? Stats { get; set; }
        public bool IsLocked { get; set; }
        public bool IsEquipped { get; set; }
        public long? EquippedHeroId { get; set; }
        public int Enhancement { get; set; }
        public int Stars { get; set; }
        public int? SlotIndex { get; set; }
    }

    public class HeroEquipmentDto
    {
        public long HeroId { get; set; }
        public InventoryItemDto? Weapon { get; set; }
        public InventoryItemDto? Armor { get; set; }
        public InventoryItemDto? Helmet { get; set; }
        public InventoryItemDto? Boots { get; set; }
        public InventoryItemDto? Ring { get; set; }
        public InventoryItemDto? Artifact { get; set; }
    }
}
