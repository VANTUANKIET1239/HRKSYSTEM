using System.Collections.Generic;

namespace GAME.Application.DTOs
{

    public class HeroStarUpgradeRequestDto
    {
        public Guid RequestId { get; set; }
        public string? MaterialType { get; set; }
    }
}
