using System.Collections.Generic;

namespace GAME.Application.DTOs
{

    public class HeroStatBreakdownDto
    {
        public string StatCode { get; set; } = null!;
        public string DisplayName { get; set; } = null!;
        public decimal Total { get; set; }

        public decimal BaseValue { get; set; }
        public decimal HeroGrowthValue { get; set; }
        public decimal EquipmentValue { get; set; }
        public decimal AuraValue { get; set; }
        public decimal OtherValue { get; set; }

        public List<HeroStatSourceDto> Sources { get; set; } = new();
    }
}
