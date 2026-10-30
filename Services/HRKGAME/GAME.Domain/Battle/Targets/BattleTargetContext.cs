namespace GAME.Domain.Battle.Targets;

public sealed class BattleTargetContext
{
    public required BattleCombatant Actor { get; init; }
    public required IReadOnlyList<BattleCombatant> Allies { get; init; }
    public required IReadOnlyList<BattleCombatant> Enemies { get; init; }
    public required Random Random { get; init; }
    public BattleSkillEffect? Effect { get; init; }
}
