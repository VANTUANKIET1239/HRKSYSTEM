using System;
using System.Collections.Generic;

namespace GAME.Application.DTOs
{

    public class ForgeEquipmentItemDto
    {
        public long InventoryItemId { get; set; }
        public int ItemTemplateId { get; set; }
        public string Code { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string? ImagePath { get; set; }
        public string? Icon { get; set; }
        public string CategoryCode { get; set; } = null!;
        public string CategoryName { get; set; } = null!;
        public string RarityCode { get; set; } = null!;
        public string RarityName { get; set; } = null!;
        public string? RarityColorHex { get; set; }
        public int RarityOrder { get; set; }
        public int LevelReq { get; set; }
        public int Enhancement { get; set; }
        public int Stars { get; set; }
        public bool IsEquipped { get; set; }
        public bool IsLocked { get; set; }
        public bool CanEnhance { get; set; }
        public string? EnhancementBlockedReasonCode { get; set; }
        public string? EnhancementBlockedMessage { get; set; }
    }
}
