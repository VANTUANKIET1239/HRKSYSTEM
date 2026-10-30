using System;
using System.Collections.Generic;

namespace GAME.Application.DTOs
{

    public class ConsumedResourcesDto
    {
        public int Gold { get; set; }
        public List<ConsumedMaterialItemDto> Stones { get; set; } = new();
        public ConsumedMaterialItemDto? Charm { get; set; }
    }
}
