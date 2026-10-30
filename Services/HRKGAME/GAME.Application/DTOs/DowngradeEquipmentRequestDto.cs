using System;
using System.Collections.Generic;

namespace GAME.Application.DTOs
{

    public class DowngradeEquipmentRequestDto
    {
        public Guid RequestId { get; set; } = Guid.NewGuid();
        public long InventoryItemId { get; set; }
        public int TargetEnhancement { get; set; }
    }
}
