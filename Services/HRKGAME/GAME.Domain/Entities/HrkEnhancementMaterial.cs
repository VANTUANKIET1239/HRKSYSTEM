using System;

namespace GAME.Domain.Entities
{
    public class HrkEnhancementMaterial
    {
        public int Id { get; set; }
        public int ItemTemplateId { get; set; }
        public string MaterialType { get; set; } = null!; // "STONE" or "CHARM"
        public decimal SuccessRateBonus { get; set; } = 0;
        public bool PreventLevelDrop { get; set; } = false;
        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedOn { get; set; } = DateTime.UtcNow;

        public virtual HrkItemTemplate ItemTemplate { get; set; } = null!;
    }
}
