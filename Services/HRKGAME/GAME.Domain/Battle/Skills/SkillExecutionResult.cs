using GAME.Domain.Battle.Effects;

namespace GAME.Domain.Battle.Skills;

public sealed class SkillExecutionResult
{
    public List<PendingBattleEvent> Events { get; } = [];
    public HashSet<long> BasicAttackHitTargetIds { get; } = [];
    public HashSet<long> DefeatedTargetIds { get; } = [];
    public List<long> TargetedCombatantIds { get; } = [];
}
