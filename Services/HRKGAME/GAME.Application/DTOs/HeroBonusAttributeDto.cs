using System.Collections.Generic;

namespace GAME.Application.DTOs
{

    public class HeroBonusAttributeDto
    {
        public int UnlockedAtStar { get; set; }
        public string Code { get; set; } = null!;
        public string Name { get; set; } = null!;
        public decimal Value { get; set; }
        public bool IsPercentage { get; set; }
    }
}
