using System.Collections.Generic;

namespace GAME.Application.DTOs
{

    public sealed class EquipmentInstanceCreationResult
    {
        public Domain.Entities.HrkPlayerInventory InventoryItem { get; set; } = null!;
        public DungeonDroppedEquipmentDto DroppedDto { get; set; } = null!;
    }
}
