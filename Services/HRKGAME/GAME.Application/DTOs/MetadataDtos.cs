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

    public class HeroFactionDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string? Icon { get; set; }
        public int DisplayOrder { get; set; }
    }

    public class HeroClassDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string? Icon { get; set; }
        public int DisplayOrder { get; set; }
    }

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

    public class LookupItemDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = null!;
        public string Name { get; set; } = null!;
        public bool? IsDebuff { get; set; }
        public int? DisplayOrder { get; set; }
    }

    public class SkillEnumsDto
    {
        public List<LookupItemDto> CostTypes { get; set; } = new();
        public List<LookupItemDto> Categories { get; set; } = new();
        public List<LookupItemDto> DamageTypes { get; set; } = new();
        public List<LookupItemDto> EffectTypes { get; set; } = new();
    }
}
