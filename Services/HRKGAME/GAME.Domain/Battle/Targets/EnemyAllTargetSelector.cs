namespace GAME.Domain.Battle.Targets;

public sealed class EnemyAllTargetSelector : BattleTargetSelectorBase
{
    public override string TargetTypeCode => BattleCodes.EnemyAll;
    public override IReadOnlyList<BattleCombatant> Select(BattleTargetContext context) => context.Enemies;
}
