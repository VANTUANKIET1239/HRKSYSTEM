namespace GAME.Domain.Entities;

public sealed class HrkSkillAnimationConfig
{
    public int Id { get; set; }
    public string SkillId { get; set; } = null!;
    public string AnimationKey { get; set; } = null!;
    public int TotalDurationMs { get; set; }
    public decimal DefaultPlaybackSpeed { get; set; } = 1m;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedOn { get; set; } = DateTime.UtcNow;
    public HrkSkillTemplate Skill { get; set; } = null!;
    public ICollection<HrkSkillTimelinePhase> Phases { get; set; } = new List<HrkSkillTimelinePhase>();
}
