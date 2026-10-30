namespace GAME.Domain.Battle.Targets;

public sealed class AllyLowestEnergyTargetSelector : BattleTargetSelectorBase
{
    public override string TargetTypeCode => BattleCodes.AllyLowestEnergy;

    public override IReadOnlyList<BattleCombatant> Select(BattleTargetContext context)
    {
        var target = context.Allies
            .Where(x => x.IsAlive)
            .OrderBy(x => x.Energy)
            .ThenBy(x => x.MaxHp > 0 ? (decimal)x.Hp / x.MaxHp : 0m)
            .ThenBy(x => x.Position)
            .ThenBy(x => x.Id)
            .FirstOrDefault();

        return target == null ? [] : [target];
    }
}
