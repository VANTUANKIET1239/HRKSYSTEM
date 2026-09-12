namespace GAME.Domain.Entities
{
    public class HrkCategoryAllowedAttribute
    {
        public int Id { get; set; }
        public int CategoryId { get; set; }
        public int AttributeTypeId { get; set; }
        public bool IsMainStat { get; set; } = false;
        public bool IsSubStat { get; set; } = false;
        public decimal? MinValue { get; set; }
        public decimal? MaxValue { get; set; }
        public int DisplayOrder { get; set; } = 0;

        public virtual HrkItemCategory Category { get; set; } = null!;
        public virtual HrkAttributeType AttributeType { get; set; } = null!;
    }
}
