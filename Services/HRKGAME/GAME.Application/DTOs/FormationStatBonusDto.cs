using System.Collections.Generic;

namespace GAME.Application.DTOs
{

    public class FormationStatBonusDto
    {
        public decimal HpPercent { get; set; } = 0m;
        public decimal AtkPercent { get; set; } = 0m;
        public decimal DefPercent { get; set; } = 0m;
        public decimal SpdPercent { get; set; } = 0m;
        public decimal MagicDamagePercent { get; set; } = 0m;
        public decimal MagicResistancePercent { get; set; } = 0m;
    }
}
