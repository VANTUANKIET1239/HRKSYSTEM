using System;
using System.Collections.Generic;

namespace GAME.Application.DTOs
{

    public class ItemCategoryDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string? Icon { get; set; }
        public int DisplayOrder { get; set; }
        public bool IsEquipment { get; set; }
        public string? Description { get; set; }
    }
}
