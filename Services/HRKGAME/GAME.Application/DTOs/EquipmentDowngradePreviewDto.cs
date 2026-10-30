using System;
using System.Collections.Generic;

namespace GAME.Application.DTOs
{

    public class EquipmentDowngradePreviewDto
    {
        public long InventoryItemId { get; set; }
        public int CurrentEnhancement { get; set; }
        public int TargetEnhancement { get; set; }
        public Dictionary<string, decimal> CurrentStats { get; set; } = new();
        public Dictionary<string, decimal> NextStats { get; set; } = new();
        public int CurrentCombatPower { get; set; }
        public int NextCombatPower { get; set; }
        public long RefundedGold { get; set; }
        public List<RefundedMaterialDto> RefundedStones { get; set; } = new();
        public List<string> NonRefundedResources { get; set; } = new();
        public bool CanDowngrade { get; set; }
        public string? ReasonCode { get; set; }
        public string? Message { get; set; }
    }
}
