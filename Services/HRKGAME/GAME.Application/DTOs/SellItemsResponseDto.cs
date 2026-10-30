using System;

namespace GAME.Application.DTOs
{

    public class SellItemsResponseDto
    {
        public long EarnedGold { get; set; }
        public long CurrentGold { get; set; }
        public int SoldItemsCount { get; set; }
    }
}
