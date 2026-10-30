using System;
using System.Collections.Generic;

namespace GAME.Application.DTOs
{

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
        public string? MessageKey { get; set; }
        public Dictionary<string, int>? MessageArguments { get; set; }
    }
}
