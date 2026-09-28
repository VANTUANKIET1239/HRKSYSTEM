using GAME.Domain.Battle.Effects;

namespace GAME.Domain.Battle.Reactions;

public sealed class BattleTargetedReactionContext
{
    public required BattleCombatant Actor { get; init; }
    public required BattleCombatant Target { get; init; }
    public required BattleSkill Skill { get; init; }
    public required string ActionId { get; init; }
    public int Round { get; init; }
    public int Turn { get; init; }
    public required IReadOnlyList<BattleCombatant> Combatants { get; init; }
    public required Random Random { get; init; }
    public int TimelineOffsetMs { get; init; }
    public string? PhaseCode { get; init; }
}

public sealed class BattleCombatantDefeatedReactionContext
{
    public required BattleCombatant DefeatedCombatant { get; init; }
    public required BattleCombatant Killer { get; init; }
    public required BattleSkill Skill { get; init; }
    public required string ActionId { get; init; }
    public int Round { get; init; }
    public int Turn { get; init; }
    public required IReadOnlyList<BattleCombatant> Combatants { get; init; }
    public required Random Random { get; init; }
    public int TimelineOffsetMs { get; init; }
    public string? PhaseCode { get; init; }
}

public interface IBattleCombatantReactionHandler
{
    IReadOnlyList<PendingBattleEvent> OnTargeted(BattleCombatant target, BattleTargetedReactionContext context);
    IReadOnlyList<PendingBattleEvent> OnCombatantDefeated(BattleCombatant observer, BattleCombatantDefeatedReactionContext context);
}
