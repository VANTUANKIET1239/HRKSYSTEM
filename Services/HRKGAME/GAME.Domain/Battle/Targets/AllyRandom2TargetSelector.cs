namespace GAME.Domain.Battle.Targets;

public sealed class AllyRandom2TargetSelector : BattleTargetSelectorBase
{
    public override string TargetTypeCode => BattleCodes.AllyRandom2;
    public override IReadOnlyList<BattleCombatant> Select(BattleTargetContext context) =>
        RandomTargets(context.Allies, 2, context.Random);
}
