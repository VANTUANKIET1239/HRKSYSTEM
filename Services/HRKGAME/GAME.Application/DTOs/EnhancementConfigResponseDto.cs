using System;
using System.Collections.Generic;

namespace GAME.Application.DTOs
{

    public class EnhancementConfigResponseDto
    {
        public List<EnhancementLevelConfigDto> LevelConfigs { get; set; } = new();
        public List<EnhancementMaterialDto> Materials { get; set; } = new();
    }
}
