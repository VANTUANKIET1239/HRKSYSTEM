using System;
using System.Collections.Generic;

namespace GAME.Domain.Entities
{
    public class HrkAttributeType
    {
        public int Id { get; set; }
        public string Code { get; set; } = null!;
        public string Name { get; set; } = null!;
        public bool IsPercentage { get; set; } = false;
        public int DisplayOrder { get; set; } = 0;
        public string? Description { get; set; }
        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedOn { get; set; } = DateTime.UtcNow;

        public virtual ICollection<HrkItemTemplateAttribute> TemplateAttributes { get; set; } = new List<HrkItemTemplateAttribute>();
        public virtual ICollection<HrkCategoryAllowedAttribute> CategoryAllowedAttributes { get; set; } = new List<HrkCategoryAllowedAttribute>();
    }
}
