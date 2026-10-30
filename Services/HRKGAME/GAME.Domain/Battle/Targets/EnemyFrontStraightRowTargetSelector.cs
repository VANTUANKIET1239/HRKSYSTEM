namespace GAME.Domain.Battle.Targets;


public sealed class EnemyFrontStraightRowTargetSelector : BattleTargetSelectorBase
{
    public override string TargetTypeCode => BattleCodes.EnemyStraightFrontRow;
    public override IReadOnlyList<BattleCombatant> Select(BattleTargetContext context)
    {
        var frontRow = context.Enemies.Where(x => x.Position is 1 or 2).ToList();
        if (frontRow.Count == 0)
        {
            frontRow = context.Enemies.Where(x => x.Position is 3).ToList();
            return frontRow.Count > 0 ? frontRow : context.Enemies.Where(x => x.Position is 4 or 5).ToList();
        }
        return frontRow;
    }
}
