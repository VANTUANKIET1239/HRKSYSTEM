namespace GAME.Domain.Battle.Targets;

public sealed class EnemyRandom4TargetSelector : BattleTargetSelectorBase
{
    public override string TargetTypeCode => BattleCodes.EnemyRandom4;
    public override IReadOnlyList<BattleCombatant> Select(BattleTargetContext context) =>
        RandomTargets(context.Enemies, 4, context.Random);
}
