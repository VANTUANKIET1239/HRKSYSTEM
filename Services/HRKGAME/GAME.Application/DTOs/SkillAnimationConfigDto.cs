using System.Collections.Generic;

namespace GAME.Application.DTOs
{

    public class SkillAnimationConfigDto
    {
        public string AnimationKey { get; set; } = null!;
        public int TotalDurationMs { get; set; }
        public decimal DefaultPlaybackSpeed { get; set; } = 1m;
        public List<SkillTimelinePhaseDto> Phases { get; set; } = new();
    }
}
