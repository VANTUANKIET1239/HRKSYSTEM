using System;

namespace GAME.Application.DTOs
{

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
