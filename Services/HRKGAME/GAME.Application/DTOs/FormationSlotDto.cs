using System.Collections.Generic;

namespace GAME.Application.DTOs
{

    public class FormationSlotDto
    {
        public int Slot { get; set; }
        public string RowType { get; set; } = "FRONT"; // FRONT, BACK
        public int Lane { get; set; }
        public int DisplayX { get; set; }
        public int DisplayY { get; set; }
        public PlayerHeroDto? Hero { get; set; }
    }
}
