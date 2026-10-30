using System;
using System.Collections.Generic;

namespace GAME.Application.DTOs
{

    public class BattleStatModifierDto
    {
        public string AttributeCode { get; set; } = null!;
        public string? AttributeName { get; set; }
        public string ValueType { get; set; } = null!;
        public decimal Value { get; set; }
    }
}
