namespace GAME.Domain.Battle.Targets;

public sealed class EnemyFrontRowFallbackTargetSelector : BattleTargetSelectorBase
{
    public override string TargetTypeCode => BattleCodes.EnemyFrontRowWithBackRowFallback;

    public override IReadOnlyList<BattleCombatant> Select(BattleTargetContext context)
    {
        var frontRow = context.Enemies
            .Where(x => x.IsAlive && x.Position is 1 or 3 or 5)
            .OrderBy(x => x.Position)
            .ThenBy(x => x.Id)
            .ToList();

        if (frontRow.Count > 0)
        {
            return frontRow;
        }

        return context.Enemies
            .Where(x => x.IsAlive && x.Position is 2 or 4)
            .OrderBy(x => x.Position)
            .ThenBy(x => x.Id)
            .ToList();
    }
}
