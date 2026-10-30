namespace GAME.Domain.Battle.Targets;

public abstract class BattleTargetSelectorBase : IBattleTargetSelector
{
    public abstract string TargetTypeCode { get; }
    public abstract IReadOnlyList<BattleCombatant> Select(BattleTargetContext context);

    protected static IReadOnlyList<BattleCombatant> RandomTargets(
        IReadOnlyList<BattleCombatant> source, int count, Random random) =>
        source.OrderBy(x => x.Position).ThenBy(x => x.Id).OrderBy(_ => random.Next()).Take(count).ToList();
}
