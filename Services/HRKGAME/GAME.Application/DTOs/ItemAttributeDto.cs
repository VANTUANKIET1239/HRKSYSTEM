using System.Collections.Generic;

namespace GAME.Application.DTOs
{

    public class ItemAttributeDto
    {
        public int AttributeTypeId { get; set; }
        public string AttributeCode { get; set; } = null!;
        public string AttributeName { get; set; } = null!;
        public bool IsPercentage { get; set; }
        public string ValueType => IsPercentage ? "PERCENT" : "FLAT";
        public decimal Value { get; set; }
        public decimal? BaseRolledValue { get; set; }
        public decimal? EnhancementValue { get; set; }
        public decimal? CurrentValue { get; set; }
        public decimal? MinValue { get; set; }
        public decimal? MaxValue { get; set; }
        public decimal? RollPercent { get; set; }
        public int DisplayOrder { get; set; }
    }
}
