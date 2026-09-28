namespace GAME.Domain.Battle.Targets;

public sealed class LowestHpPercentTargetSelector : BattleTargetSelectorBase
{
    public override string TargetTypeCode => BattleCodes.LowestHpPercent;

    public override IReadOnlyList<BattleCombatant> Select(BattleTargetContext context)
    {
        var targetSide = context.Effect?.GetString("TARGET_SIDE", string.Empty);
        IReadOnlyList<BattleCombatant> pool;

        if (string.Equals(targetSide, "ALLY", StringComparison.OrdinalIgnoreCase))
        {
            pool = context.Allies;
        }
        else if (string.Equals(targetSide, "ENEMY", StringComparison.OrdinalIgnoreCase))
        {
            pool = context.Enemies;
        }
        else if (context.Effect != null && IsBeneficialEffect(context.Effect.EffectTypeCode))
        {
            pool = context.Allies;
        }
        else if (context.Enemies.Count > 0)
        {
            pool = context.Enemies;
        }
        else
        {
            pool = context.Allies;
        }

        var target = pool
            .Where(e => e.IsAlive)
            .OrderBy(e => (decimal)e.Hp / Math.Max(1, e.MaxHp))
            .ThenBy(e => e.Position)
            .ThenBy(e => e.Id)
            .FirstOrDefault();

        return target != null ? [target] : [];
    }

    private static bool IsBeneficialEffect(string effectTypeCode) =>
        effectTypeCode.Equals(BattleCodes.Heal, StringComparison.OrdinalIgnoreCase) ||
        effectTypeCode.Equals(BattleCodes.Shield, StringComparison.OrdinalIgnoreCase) ||
        effectTypeCode.Equals(BattleCodes.StatBuff, StringComparison.OrdinalIgnoreCase);
}
