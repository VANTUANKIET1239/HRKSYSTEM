namespace GAME.Domain.Battle.Targets;

public sealed class AllyRandomTargetSelector : BattleTargetSelectorBase
{
    public override string TargetTypeCode => BattleCodes.AllyRandom;
    public override IReadOnlyList<BattleCombatant> Select(BattleTargetContext context) =>
        RandomTargets(context.Allies, 1, context.Random);
}
