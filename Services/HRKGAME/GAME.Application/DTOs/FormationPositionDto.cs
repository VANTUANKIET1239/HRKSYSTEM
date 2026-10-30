using System.Collections.Generic;

namespace GAME.Application.DTOs
{
    public class FormationPositionDto
    {
        public int Slot { get; set; }
        public PlayerHeroDto? Hero { get; set; }
    }
}
