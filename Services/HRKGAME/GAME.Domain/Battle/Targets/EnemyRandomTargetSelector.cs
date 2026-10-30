namespace GAME.Domain.Battle.Targets;

public sealed class EnemyRandomTargetSelector : BattleTargetSelectorBase
{
    public override string TargetTypeCode => BattleCodes.EnemyRandom;
    public override IReadOnlyList<BattleCombatant> Select(BattleTargetContext context) =>
        RandomTargets(context.Enemies, 1, context.Random);
}
