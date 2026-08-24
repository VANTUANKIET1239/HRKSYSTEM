using System;
using System.Collections.Generic;

namespace GAME.Domain.Entities
{
    public class HrkHeroClass
    {
        public int Id { get; set; }
        public string Code { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string? Icon { get; set; }
        public int DisplayOrder { get; set; }
        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;

        public virtual ICollection<HrkHeroTemplate> HeroTemplates { get; set; } = new List<HrkHeroTemplate>();
    }
}
