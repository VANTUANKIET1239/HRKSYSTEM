using System;
using System.Collections.Generic;

namespace GAME.Domain.Entities
{
    public class HrkRarity
    {
        public int Id { get; set; }
        public string Code { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string? ColorHex { get; set; }
        public int DisplayOrder { get; set; }
        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;

        public virtual ICollection<HrkHeroTemplate> HeroTemplates { get; set; } = new List<HrkHeroTemplate>();
        public virtual ICollection<HrkItemTemplate> ItemTemplates { get; set; } = new List<HrkItemTemplate>();
    }
}
