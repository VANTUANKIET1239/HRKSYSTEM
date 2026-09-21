using System;

namespace GAME.Domain.Entities
{
    public class HrkHeroStarAuraConfig
    {
        public long Id { get; set; }
        public int HeroTemplateId { get; set; }
        public byte StarLevel { get; set; }
        public string AuraCode { get; set; } = null!;
        public string VisualKey { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public string? PrimaryColorHex { get; set; }
        public string? SecondaryColorHex { get; set; }
        public decimal Intensity { get; set; }
        public byte ParticleLevel { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedOn { get; set; } = DateTime.UtcNow;

        public virtual HrkHeroTemplate HeroTemplate { get; set; } = null!;
    }
}
