using System;
using System.Collections.Generic;

namespace GAME.Application.DTOs
{
    public class EnhanceEquipmentRequestDto
    {
        public Guid RequestId { get; set; } = Guid.NewGuid();
        public long InventoryItemId { get; set; }
        public List<long> StoneInventoryItemIds { get; set; } = new();
        public long? CharmInventoryItemId { get; set; }
    }
}
