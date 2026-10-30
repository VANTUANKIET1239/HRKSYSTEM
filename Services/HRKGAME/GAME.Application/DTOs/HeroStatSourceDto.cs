using System.Collections.Generic;

namespace GAME.Application.DTOs
{

    public class HeroStatSourceDto
    {
        public string SourceType { get; set; } = null!; // BASE, HERO_GROWTH, EQUIPMENT, AURA, OTHER
        public string SourceName { get; set; } = null!;
        public string? ItemName { get; set; }
        public long? InventoryItemId { get; set; }
        public decimal Value { get; set; }
    }
}
