using System;
using System.Collections.Generic;

namespace GAME.Application.DTOs
{

    public class RefundedMaterialDto
    {
        public int ItemTemplateId { get; set; }
        public string Code { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string? ImagePath { get; set; }
        public int Quantity { get; set; }
    }
}
