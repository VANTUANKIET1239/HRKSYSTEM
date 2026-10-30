using System.Collections.Generic;

namespace GAME.Application.DTOs
{

    public class FormationPreviewRequestDto
    {
        public string FormationCode { get; set; } = null!;
        public List<FormationPositionRequestDto> Positions { get; set; } = new();
    }
}
