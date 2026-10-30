using System.Collections.Generic;

namespace GAME.Application.DTOs
{

    public class PlayerHeroDetailDto : PlayerHeroDto
    {
        public HeroEquipmentDto Equipment { get; set; } = new();
        public List<HeroStatBreakdownDto> StatBreakdowns { get; set; } = new();
        public List<HeroBonusAttributeDto> StarBonusAttributes { get; set; } = new();
    }
}
