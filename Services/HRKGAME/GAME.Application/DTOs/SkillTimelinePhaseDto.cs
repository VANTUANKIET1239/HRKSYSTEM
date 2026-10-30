using System.Collections.Generic;

namespace GAME.Application.DTOs
{

    public class SkillTimelinePhaseDto
    {
        public string PhaseCode { get; set; } = null!;
        public int StartAtMs { get; set; }
        public int DurationMs { get; set; }
        public string? TriggerEventType { get; set; }
        public int DisplayOrder { get; set; }
    }
}
