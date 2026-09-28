namespace GAME.Domain.Battle.Targets;

public sealed class AllyLowestHpPreferWithoutStatusTargetSelector : BattleTargetSelectorBase
{
    public override string TargetTypeCode => BattleCodes.AllyLowestHpPreferWithoutStatus;

    public override IReadOnlyList<BattleCombatant> Select(BattleTargetContext context)
    {
        var livingAllies = context.Allies.Where(a => a.IsAlive).ToList();
        if (livingAllies.Count == 0) return [];

        var statusGroup = context.Effect?.GetString("PREFERRED_MISSING_STATUS_GROUP", string.Empty);
        var targetCount = context.Effect?.GetInt("TARGET_COUNT", 1) ?? 1;

        IReadOnlyList<BattleCombatant> pool = livingAllies;
        if (!string.IsNullOrWhiteSpace(statusGroup))
        {
            var withoutStatus = livingAllies.Where(a => !HasStatusInGroup(a, statusGroup)).ToList();
            if (withoutStatus.Count > 0)
            {
                pool = withoutStatus;
            }
        }

        // Priority ordering:
        // 1. Lowest % HP
        // 2. Lowest absolute HP
        // 3. Formation position
        // 4. Combatant ID
        var selected = pool
            .OrderBy(a => (decimal)a.Hp / Math.Max(1, a.MaxHp))
            .ThenBy(a => a.Hp)
            .ThenBy(a => a.Position)
            .ThenBy(a => a.Id)
            .Take(targetCount)
            .ToList();

        return selected;
    }

    public static bool HasStatusInGroup(BattleCombatant combatant, string group)
    {
        if (string.IsNullOrWhiteSpace(group)) return false;

        return combatant.StatusEffects.Any(s =>
            s.IsActive &&
            (
                string.Equals(s.StatusGroup, group, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(s.EffectTypeCode, group, StringComparison.OrdinalIgnoreCase) ||
                s.EffectTypeCode.StartsWith(group + "_", StringComparison.OrdinalIgnoreCase)
            ));
    }
}
