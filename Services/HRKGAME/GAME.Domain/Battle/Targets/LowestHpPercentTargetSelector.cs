namespace GAME.Domain.Battle.Targets;

public sealed class LowestHpPercentTargetSelector : BattleTargetSelectorBase
{
    public override string TargetTypeCode => BattleCodes.LowestHpPercent;

    public override IReadOnlyList<BattleCombatant> Select(BattleTargetContext context)
    {
        var target = context.Enemies
            .Where(e => e.IsAlive)
            .OrderBy(e => (decimal)e.Hp / Math.Max(1, e.MaxHp))
            .ThenBy(e => e.Position)
            .ThenBy(e => e.Id)
            .FirstOrDefault();

        return target != null ? [target] : [];
    }
}
