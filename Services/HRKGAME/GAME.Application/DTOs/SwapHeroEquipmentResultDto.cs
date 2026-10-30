using System.Collections.Generic;

namespace GAME.Application.DTOs
{

    public class SwapHeroEquipmentResultDto
    {
        public PlayerHeroDetailDto SourceHero { get; set; } = null!;
        public PlayerHeroDetailDto TargetHero { get; set; } = null!;
    }
}
