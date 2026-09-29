namespace GAME.Domain.Battle.Targets;

public sealed class RandomEligibleAlliesNTargetSelector : BattleTargetSelectorBase
{
    public override string TargetTypeCode => BattleCodes.RandomEligibleAlliesN;

    public override IReadOnlyList<BattleCombatant> Select(BattleTargetContext context)
    {
        var count = context.Effect?.GetInt("TARGET_COUNT", 3) ?? 3;
        var excludeHealerBasics = context.Effect?.GetBool("EXCLUDE_HEALER_BASICS", true) ?? true;
        var excludeActor = context.Effect?.GetBool("EXCLUDE_ACTOR", true) ?? true;
        var requiredAbsentStatus = context.Effect?.GetString("REQUIRED_ABSENT_STATUS_CODE", BattleCodes.CatCompanion);

        var eligible = context.Allies
            .Where(ally => ally.IsAlive)
            .Where(ally => !excludeActor || ally.Id != context.Actor.Id)
            .Where(ally => !excludeHealerBasics || !ally.BasicSkill.Effects.Any(e =>
                e.EffectTypeCode.Equals(BattleCodes.Heal, StringComparison.OrdinalIgnoreCase)))
            .Where(ally => string.IsNullOrEmpty(requiredAbsentStatus) ||
                !ally.StatusEffects.Any(s => s.RemainingTurns > 0 &&
                    s.EffectTypeCode.Equals(requiredAbsentStatus, StringComparison.OrdinalIgnoreCase)))
            .OrderBy(ally => ally.Position)
            .ThenBy(ally => ally.Id)
            .ToList();

        if (eligible.Count <= count)
        {
            return eligible;
        }

        return RandomTargets(eligible, count, context.Random);
    }
}
