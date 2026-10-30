namespace GAME.Domain.Battle.Targets;

public sealed class EnemyRandom3TargetSelector : BattleTargetSelectorBase
{
    public override string TargetTypeCode => "ENEMY_RANDOM_3";

    public override IReadOnlyList<BattleCombatant> Select(BattleTargetContext context)
    {
        var livingEnemies = context.Enemies.Where(x => x.IsAlive).ToList();
        if (livingEnemies.Count <= 3)
        {
            return livingEnemies;
        }

        return RandomTargets(livingEnemies, 3, context.Random);
    }
}
