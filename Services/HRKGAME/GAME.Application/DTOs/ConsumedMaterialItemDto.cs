using System;
using System.Collections.Generic;

namespace GAME.Application.DTOs
{

    public class ConsumedMaterialItemDto
    {
        public int ItemTemplateId { get; set; }
        public string Code { get; set; } = null!;
        public string Name { get; set; } = null!;
        public int Quantity { get; set; }
        public decimal SuccessRateBonus { get; set; }
    }
}
