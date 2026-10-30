namespace GAME.Domain.Battle;

public sealed record BattleSkillTimelinePhase(
    string PhaseCode, int StartAtMs, int DurationMs, string? TriggerEventType);
