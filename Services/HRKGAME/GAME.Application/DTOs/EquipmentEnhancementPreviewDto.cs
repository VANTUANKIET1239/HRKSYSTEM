using System;
using System.Collections.Generic;

namespace GAME.Application.DTOs
{

    public class EquipmentEnhancementPreviewDto
    {
        public long InventoryItemId { get; set; }
        public int CurrentEnhancement { get; set; }
        public int TargetEnhancement { get; set; }
        public Dictionary<string, decimal> CurrentStats { get; set; } = new();
        public Dictionary<string, decimal> NextStats { get; set; } = new();
        public decimal BaseSuccessRate { get; set; }
        public int GoldCost { get; set; }
        public int FailureDropLevels { get; set; }
        public int MaxStoneSlots { get; set; }
        public bool CanEnhance { get; set; }
        public string? ReasonCode { get; set; }
        public string? Message { get; set; }
    }
}
