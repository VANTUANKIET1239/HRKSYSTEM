namespace GAME.Domain.Battle.Targets;

public sealed class EnemyBackRowTargetSelector : BattleTargetSelectorBase
{
    public override string TargetTypeCode => BattleCodes.EnemyBackRow;

    public override IReadOnlyList<BattleCombatant> Select(BattleTargetContext context)
    {
        var backRow = context.Enemies.Where(x => x.Position is 2 or 4).ToList();
        return backRow.Count > 0
            ? backRow
            : context.Enemies.Where(x => x.Position is 1 or 3 or 5).ToList();
    }
}
