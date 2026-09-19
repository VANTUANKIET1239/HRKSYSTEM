using System;

namespace GAME.Domain.Entities
{
    public class HrkFormationLevelConfig
    {
        public int Id { get; set; }
        public int FormationTemplateId { get; set; }
        public int Level { get; set; }
        public long GoldCost { get; set; }
        public int StoneItemTemplateId { get; set; }
        public int StoneCost { get; set; }
        public string StatBonusJson { get; set; } = "{}";
        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedOn { get; set; } = DateTime.UtcNow;

        public virtual HrkFormationTemplate FormationTemplate { get; set; } = null!;
        public virtual HrkItemTemplate StoneItemTemplate { get; set; } = null!;
    }
}
