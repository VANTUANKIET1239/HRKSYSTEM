namespace GAME.Domain.Battle.Targets;

public sealed class EnemyRandomDistinctNTargetSelector : BattleTargetSelectorBase
{
    public override string TargetTypeCode => BattleCodes.EnemyRandomDistinctN;

    public override IReadOnlyList<BattleCombatant> Select(BattleTargetContext context)
    {
        var count = context.Effect?.GetInt("TARGET_COUNT", 0) ?? 0;
        if (count <= 0)
        {
            count = context.Effect?.GetInt("N", 3) ?? 3;
        }

        var livingEnemies = context.Enemies.Where(x => x.IsAlive).ToList();
        if (livingEnemies.Count <= count)
        {
            return livingEnemies;
        }

        return RandomTargets(livingEnemies, count, context.Random);
    }
}
