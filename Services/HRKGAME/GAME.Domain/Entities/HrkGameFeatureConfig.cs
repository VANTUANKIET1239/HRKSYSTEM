using System;
using System.Collections.Generic;

namespace GAME.Domain.Entities
{
    public class HrkGameFeatureConfig
    {
        public int Id { get; set; }
        public string Code { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string? Icon { get; set; }
        public int? ParentFeatureId { get; set; }
        public string Placement { get; set; } = "NONE";
        public string? ActionCode { get; set; }
        public int DisplayOrder { get; set; }
        public bool IsEnabled { get; set; } = true;
        public bool IsLocked { get; set; }
        public bool HasNotification { get; set; }
        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedOn { get; set; } = DateTime.UtcNow;

        public virtual HrkGameFeatureConfig? ParentFeature { get; set; }
        public virtual ICollection<HrkGameFeatureConfig> Children { get; set; } = new List<HrkGameFeatureConfig>();
    }
}
