namespace GAME.Domain.Battle.Targets;


public sealed class EnemyFrontRowTargetSelector : BattleTargetSelectorBase
{
    public override string TargetTypeCode => BattleCodes.EnemyFrontRow;

    public override IReadOnlyList<BattleCombatant> Select(BattleTargetContext context)
    {
        var frontRow = context.Enemies.Where(x => x.Position is 1 or 3 or 5).ToList();
        return frontRow.Count > 0
            ? frontRow
            : context.Enemies.Where(x => x.Position is 2 or 4).ToList();
    }
}
