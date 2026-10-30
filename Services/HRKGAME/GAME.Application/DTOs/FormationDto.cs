using System.Collections.Generic;

namespace GAME.Application.DTOs
{

    public class FormationDto
    {
        public long PlayerId { get; set; }
        public string FormationName { get; set; } = "Main Team";
        public int TotalPower { get; set; }
        public List<FormationPositionDto> Positions { get; set; } = new();
    }
}
