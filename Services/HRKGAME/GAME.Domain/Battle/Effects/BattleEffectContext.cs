namespace GAME.Domain.Battle.Effects;

public sealed class BattleEffectContext
{
    public required BattleSkillEffect Effect { get; init; }
    public required BattleSkill Skill { get; init; }
    public required BattleCombatant Actor { get; init; }
    public required BattleCombatant Target { get; init; }
    public required IReadOnlyList<BattleCombatant> SelectedTargets { get; init; }
    public required IReadOnlyList<BattleCombatant> Combatants { get; init; }
    public required Random Random { get; init; }
    public int Round { get; init; }
    public int Turn { get; init; }
    public string? ActionId { get; init; }
    public int TimelineOffsetMs { get; init; }
    public string? PhaseCode { get; init; }
}
