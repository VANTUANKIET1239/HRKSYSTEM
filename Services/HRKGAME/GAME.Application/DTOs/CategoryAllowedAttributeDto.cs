using System.Collections.Generic;

namespace GAME.Application.DTOs
{

    public class CategoryAllowedAttributeDto
    {
        public int Id { get; set; }
        public int CategoryId { get; set; }
        public int AttributeTypeId { get; set; }
        public string AttributeCode { get; set; } = null!;
        public string AttributeName { get; set; } = null!;
        public bool IsMainStat { get; set; }
        public bool IsSubStat { get; set; }
        public decimal? MinValue { get; set; }
        public decimal? MaxValue { get; set; }
        public int DisplayOrder { get; set; }
    }
}
