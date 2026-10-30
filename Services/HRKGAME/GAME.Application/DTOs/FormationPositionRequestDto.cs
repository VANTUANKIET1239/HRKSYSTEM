using System.Collections.Generic;

namespace GAME.Application.DTOs
{

    public class FormationPositionRequestDto
    {
        public int Slot { get; set; }
        public long? HeroId { get; set; }
    }
}
