using System.Collections.Generic;

namespace GAME.Application.DTOs
{

    public class UpdateFormationPositionsRequest
    {
        public List<SlotHeroPositionDto> Positions { get; set; } = new();
    }
}
