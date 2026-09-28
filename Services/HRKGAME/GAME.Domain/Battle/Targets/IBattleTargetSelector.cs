namespace GAME.Domain.Battle.Targets;

public interface IBattleTargetSelector
{
    string TargetTypeCode { get; }
    IReadOnlyList<BattleCombatant> Select(BattleTargetContext context);
}

public sealed class BattleTargetContext
{
    public required BattleCombatant Actor { get; init; }
    public required IReadOnlyList<BattleCombatant> Allies { get; init; }
    public required IReadOnlyList<BattleCombatant> Enemies { get; init; }
    public required Random Random { get; init; }
    public BattleSkillEffect? Effect { get; init; }
}

public abstract class BattleTargetSelectorBase : IBattleTargetSelector
{
    public abstract string TargetTypeCode { get; }
    public abstract IReadOnlyList<BattleCombatant> Select(BattleTargetContext context);

    protected static IReadOnlyList<BattleCombatant> RandomTargets(
        IReadOnlyList<BattleCombatant> source, int count, Random random) =>
        source.OrderBy(x => x.Position).ThenBy(x => x.Id).OrderBy(_ => random.Next()).Take(count).ToList();
}
