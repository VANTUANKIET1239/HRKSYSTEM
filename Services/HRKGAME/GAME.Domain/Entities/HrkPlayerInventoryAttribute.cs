using System;

namespace GAME.Domain.Entities
{
    public class HrkPlayerInventoryAttribute
    {
        public long Id { get; set; }
        public long PlayerInventoryId { get; set; }
        public int AttributeTypeId { get; set; }
        public decimal BaseRolledValue { get; set; }
        public decimal CurrentValue { get; set; }
        public decimal RollMinValue { get; set; }
        public decimal RollMaxValue { get; set; }
        public decimal RollQualityPercent { get; set; }
        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedOn { get; set; } = DateTime.UtcNow;

        public virtual HrkPlayerInventory PlayerInventory { get; set; } = null!;
        public virtual HrkAttributeType AttributeType { get; set; } = null!;
    }
}
