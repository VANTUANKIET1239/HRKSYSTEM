namespace GAME.Domain.Entities
{
    public class HrkItemTemplateAttribute
    {
        public long Id { get; set; }
        public int ItemTemplateId { get; set; }
        public int AttributeTypeId { get; set; }
        public decimal Value { get; set; }
        public decimal? MinValue { get; set; }
        public decimal? MaxValue { get; set; }

        public virtual HrkItemTemplate ItemTemplate { get; set; } = null!;
        public virtual HrkAttributeType AttributeType { get; set; } = null!;
    }
}
