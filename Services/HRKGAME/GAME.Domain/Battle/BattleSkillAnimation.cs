namespace GAME.Domain.Battle;

public sealed class BattleSkillAnimation
{
    public static BattleSkillAnimation Default { get; } = new();
    public string AnimationKey { get; init; } = "basic";
    public int TotalDurationMs { get; init; } = 2400;
    public IReadOnlyList<BattleSkillTimelinePhase> Phases { get; init; } =
    [
        new("CAST", 0, 800, "SKILL_CAST"),
        new("IMPACT", 800, 800, "DAMAGE"),
        new("STATUS", 1600, 0, "STATUS_APPLIED"),
        new("RECOVERY", 1600, 800, "SKILL_COMPLETED")
    ];

    public BattleSkillTimelinePhase Phase(string code) =>
        Phases.FirstOrDefault(x => x.PhaseCode.Equals(code, StringComparison.OrdinalIgnoreCase))
        ?? new BattleSkillTimelinePhase(code, 0, 0, null);
}
