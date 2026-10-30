using GAME.Domain.Battle.Effects;
using GAME.Domain.Battle.Skills;

namespace GAME.Domain.Battle.Reactions;

public sealed class BattleActionCompletedReactionContext
{
    public required BattleCombatant Actor { get; init; }
    public required BattleSkill Skill { get; init; }
    public required SkillExecutionResult ExecutionResult { get; init; }
    public required string ActionId { get; init; }
    public int Round { get; init; }
    public int Turn { get; init; }
    public required IReadOnlyList<BattleCombatant> Combatants { get; init; }
    public required Random Random { get; init; }
    public int TimelineOffsetMs { get; init; }
    public string? PhaseCode { get; init; }
}
