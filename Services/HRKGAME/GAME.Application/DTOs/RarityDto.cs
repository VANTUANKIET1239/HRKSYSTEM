using System;
using System.Collections.Generic;

namespace GAME.Application.DTOs
{
    public class RarityDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string? ColorHex { get; set; }
        public int DisplayOrder { get; set; }
    }
}
