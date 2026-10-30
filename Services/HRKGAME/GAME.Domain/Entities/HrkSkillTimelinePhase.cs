namespace GAME.Domain.Entities;

public sealed class HrkSkillTimelinePhase
{
    public long Id { get; set; }
    public int SkillAnimationConfigId { get; set; }
    public string PhaseCode { get; set; } = null!;
    public int StartAtMs { get; set; }
    public int DurationMs { get; set; }
    public string? TriggerEventType { get; set; }
    public int DisplayOrder { get; set; }
    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
    public HrkSkillAnimationConfig SkillAnimationConfig { get; set; } = null!;
}
