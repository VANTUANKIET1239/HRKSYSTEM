using System;
using System.Collections.Generic;

namespace GAME.Domain.Entities
{
    public class HrkItemCategory
    {
        public int Id { get; set; }
        public string Code { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string? Icon { get; set; }
        public int DisplayOrder { get; set; }
        public bool IsEquipment { get; set; }
        public string? Description { get; set; }
        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedOn { get; set; } = DateTime.UtcNow;

        public virtual ICollection<HrkItemTemplate> ItemTemplates { get; set; } = new List<HrkItemTemplate>();
    }
}
