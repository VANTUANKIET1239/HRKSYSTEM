using System;
using System.Collections.Generic;

namespace GAME.Application.DTOs
{

    public class SkillEnumsDto
    {
        public List<LookupItemDto> CostTypes { get; set; } = new();
        public List<LookupItemDto> Categories { get; set; } = new();
        public List<LookupItemDto> DamageTypes { get; set; } = new();
        public List<LookupItemDto> EffectTypes { get; set; } = new();
    }
}
