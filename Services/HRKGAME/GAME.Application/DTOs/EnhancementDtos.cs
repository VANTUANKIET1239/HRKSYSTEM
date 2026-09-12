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

    public class EnhanceEquipmentResultDto
    {
        public Guid RequestId { get; set; }
        public bool Success { get; set; }
        public int OldEnhancement { get; set; }
        public int TargetEnhancement { get; set; }
        public int NewEnhancement { get; set; }

        public decimal BaseSuccessRate { get; set; }
        public decimal StoneBonusRate { get; set; }
        public decimal CharmBonusRate { get; set; }
        public decimal FinalSuccessRate { get; set; }

        public bool WasLevelProtected { get; set; }
        public Dictionary<string, decimal> CurrentStats { get; set; } = new();
        public string? CurrentStatsJson { get; set; }
        public ConsumedResourcesDto Consumed { get; set; } = new();
        public string Message { get; set; } = string.Empty;
    }

    public class ConsumedResourcesDto
    {
        public int Gold { get; set; }
        public List<ConsumedMaterialItemDto> Stones { get; set; } = new();
        public ConsumedMaterialItemDto? Charm { get; set; }
    }

    public class ConsumedMaterialItemDto
    {
        public int ItemTemplateId { get; set; }
        public string Code { get; set; } = null!;
        public string Name { get; set; } = null!;
        public int Quantity { get; set; }
        public decimal SuccessRateBonus { get; set; }
    }

    public class EnhancementLevelConfigDto
    {
        public int CurrentLevel { get; set; }
        public int NextLevel { get; set; }
        public decimal BaseSuccessRate { get; set; }
        public int GoldCost { get; set; }
        public int FailureDropLevels { get; set; }
        public int MaxStoneSlots { get; set; }
    }

    public class EnhancementMaterialDto
    {
        public int ItemTemplateId { get; set; }
        public string Code { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string? Icon { get; set; }
        public string? ImagePath { get; set; }
        public string RarityCode { get; set; } = "Common";
        public string RarityName { get; set; } = "Thường";
        public string? RarityColorHex { get; set; }
        public string MaterialType { get; set; } = "STONE"; // STONE, CHARM
        public decimal SuccessRateBonus { get; set; }
        public bool PreventLevelDrop { get; set; }
        public string? Description { get; set; }
    }

    public class EnhancementConfigResponseDto
    {
        public List<EnhancementLevelConfigDto> LevelConfigs { get; set; } = new();
        public List<EnhancementMaterialDto> Materials { get; set; } = new();
    }
}
