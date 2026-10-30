using System.Collections.Generic;

namespace GAME.Application.DTOs
{

    public class HeroStarAuraConfigDto
    {
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
    }
}
