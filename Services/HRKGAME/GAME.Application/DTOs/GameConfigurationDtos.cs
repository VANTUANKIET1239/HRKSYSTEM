using System.Collections.Generic;

namespace GAME.Application.DTOs
{
    public class GameFeatureConfigDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string? Icon { get; set; }
        public int? ParentFeatureId { get; set; }
        public string Placement { get; set; } = null!;
        public string? ActionCode { get; set; }
        public int DisplayOrder { get; set; }
        public bool IsLocked { get; set; }
        public bool HasNotification { get; set; }
        public List<GameFeatureConfigDto> Children { get; set; } = new();
    }

    public class CombatPowerConfigDto
    {
        public string StatCode { get; set; } = null!;
        public decimal PowerPerUnit { get; set; }
        public bool IsEnabled { get; set; }
        public int DisplayOrder { get; set; }
    }
}
