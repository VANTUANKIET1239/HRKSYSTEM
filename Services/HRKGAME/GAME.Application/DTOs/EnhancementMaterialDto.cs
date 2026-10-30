using System;
using System.Collections.Generic;

namespace GAME.Application.DTOs
{

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
}
