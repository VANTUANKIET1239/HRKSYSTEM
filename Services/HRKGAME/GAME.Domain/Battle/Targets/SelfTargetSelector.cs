namespace GAME.Domain.Battle.Targets;

public sealed class SelfTargetSelector : BattleTargetSelectorBase
{
    public override string TargetTypeCode => BattleCodes.Self;
    public override IReadOnlyList<BattleCombatant> Select(BattleTargetContext context) => [context.Actor];
}
