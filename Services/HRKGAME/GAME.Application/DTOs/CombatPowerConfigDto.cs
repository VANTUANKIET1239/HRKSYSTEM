using System.Collections.Generic;

namespace GAME.Application.DTOs
{

    public class CombatPowerConfigDto
    {
        public string StatCode { get; set; } = null!;
        public decimal PowerPerUnit { get; set; }
        public bool IsEnabled { get; set; }
        public int DisplayOrder { get; set; }
    }
}
