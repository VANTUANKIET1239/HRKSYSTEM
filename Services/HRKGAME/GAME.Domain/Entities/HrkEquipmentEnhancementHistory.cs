using System;

namespace GAME.Domain.Entities
{
    public class HrkEquipmentEnhancementHistory
    {
        public long Id { get; set; }
        public Guid RequestId { get; set; }
        public long PlayerId { get; set; }
        public long PlayerInventoryId { get; set; }
        public int ItemTemplateId { get; set; }

        public int OldEnhancement { get; set; }
        public int TargetEnhancement { get; set; }
        public int NewEnhancement { get; set; }

        public decimal BaseSuccessRate { get; set; }
        public decimal StoneBonusRate { get; set; }
        public decimal CharmBonusRate { get; set; }
        public decimal FinalSuccessRate { get; set; }

        public bool IsSuccess { get; set; }

        public int FailureDropLevels { get; set; }
        public bool WasLevelProtected { get; set; }

        public int GoldCost { get; set; }

        public string? UsedMaterialsJson { get; set; }

        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;

        public virtual HrkPlayer Player { get; set; } = null!;
        public virtual HrkPlayerInventory PlayerInventory { get; set; } = null!;
        public virtual HrkItemTemplate ItemTemplate { get; set; } = null!;
    }
}
