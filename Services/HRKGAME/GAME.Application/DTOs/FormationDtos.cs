using System.Collections.Generic;

namespace GAME.Application.DTOs
{
    public class FormationPositionDto
    {
        public int Slot { get; set; }
        public PlayerHeroDto? Hero { get; set; }
    }

    public class FormationDto
    {
        public long PlayerId { get; set; }
        public string FormationName { get; set; } = null!;
        public int TotalPower { get; set; }
        public List<FormationPositionDto> Positions { get; set; } = new();
    }
}
