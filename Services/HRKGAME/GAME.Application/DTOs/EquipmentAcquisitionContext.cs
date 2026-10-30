using System.Collections.Generic;

namespace GAME.Application.DTOs
{
    public sealed class EquipmentAcquisitionContext
    {
        public string Source { get; set; } = "DUNGEON_DROP";
        public string? ClientRequestId { get; set; }
        public int? StageId { get; set; }
    }
}
