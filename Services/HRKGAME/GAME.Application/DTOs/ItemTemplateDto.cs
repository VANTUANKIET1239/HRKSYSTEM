using System.Collections.Generic;

namespace GAME.Application.DTOs
{

    public class ItemTemplateDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = null!;
        public int CategoryId { get; set; }
        public string CategoryCode { get; set; } = null!;
        public string CategoryName { get; set; } = null!;
        public int RarityId { get; set; }
        public string RarityCode { get; set; } = null!;
        public string RarityName { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string? ImagePath { get; set; }
        public string? Icon { get; set; }
        public int LevelReq { get; set; }
        public string? Description { get; set; }
        public string? MetadataJson { get; set; }
        public object? BaseStats { get; set; }
        public List<ItemAttributeDto> Attributes { get; set; } = new();
        public bool IsStackable { get; set; }
        public int MaxStackSize { get; set; }
        public int SellPrice { get; set; }
    }
}
