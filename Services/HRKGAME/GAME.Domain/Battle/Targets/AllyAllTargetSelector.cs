namespace GAME.Domain.Battle.Targets;

public sealed class AllyAllTargetSelector : BattleTargetSelectorBase
{
    public override string TargetTypeCode => BattleCodes.AllyAll;
    public override IReadOnlyList<BattleCombatant> Select(BattleTargetContext context) => context.Allies;
}
