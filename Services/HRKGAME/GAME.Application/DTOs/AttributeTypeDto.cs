using System.Collections.Generic;

namespace GAME.Application.DTOs
{

    public class AttributeTypeDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = null!;
        public string Name { get; set; } = null!;
        public bool IsPercentage { get; set; }
        public int DisplayOrder { get; set; }
        public string? Description { get; set; }
    }
}
