using System.Collections.Generic;

namespace GAME.Application.DTOs
{

    public class UpgradeHeroRequestDto
    {
        /// <summary>Number of levels to buy. The server caps this at 50.</summary>
        public int Levels { get; set; } = 1;
    }
}
