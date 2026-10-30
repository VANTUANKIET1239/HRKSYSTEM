using System.Collections.Generic;

namespace GAME.Application.DTOs
{

    public class StarMaterialRequirementDto
    {
        public int ItemTemplateId { get; set; }
        public string Name { get; set; } = null!;
        public string? ImagePath { get; set; }
        public int Required { get; set; }
        public int Owned { get; set; }
    }
}
