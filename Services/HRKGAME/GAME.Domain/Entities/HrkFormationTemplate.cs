using System;
using System.Collections.Generic;

namespace GAME.Domain.Entities
{
    public class HrkFormationTemplate
    {
        public int Id { get; set; }
        public string Code { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public string? ImagePath { get; set; }
        public int MaxLevel { get; set; } = 5;
        public string? UnlockConditionJson { get; set; }
        public int DisplayOrder { get; set; }
        public bool IsEnabled { get; set; } = true;
        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedOn { get; set; } = DateTime.UtcNow;

        public virtual ICollection<HrkFormationSlotTemplate> Slots { get; set; } = new List<HrkFormationSlotTemplate>();
        public virtual ICollection<HrkFormationLevelConfig> LevelConfigs { get; set; } = new List<HrkFormationLevelConfig>();
        public virtual ICollection<HrkPlayerFormation> PlayerFormations { get; set; } = new List<HrkPlayerFormation>();
    }
}
