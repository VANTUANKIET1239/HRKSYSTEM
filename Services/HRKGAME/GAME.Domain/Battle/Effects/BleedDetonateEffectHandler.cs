namespace GAME.Domain.Battle.Effects;

public sealed class BleedDetonateEffectHandler : IBattleEffectHandler
{
    public string EffectTypeCode => BattleCodes.BleedDetonate;

    public IReadOnlyList<PendingBattleEvent> Apply(BattleEffectContext context)
    {
        var requiredStatus = context.Effect.GetString("REQUIRED_STATUS_CODE", BattleCodes.Bleed);
        var multiplier = context.Effect.GetRequiredDecimal("DETONATION_MULTIPLIER", context.Skill.Id);
        var armorIgnore = context.Effect.GetDecimal("ARMOR_IGNORE_PERCENT", 0m);
        var canCrit = context.Effect.GetBool("CAN_CRIT", false);
        var removeStatus = context.Effect.GetBool("REMOVE_STATUS_AFTER_EXECUTION", true);
        var school = context.Effect.DamageSchoolCode ?? BattleCodes.Physical;

        var existingStatuses = context.Target.StatusEffects
            .Where(x => x.EffectTypeCode.Equals(requiredStatus, StringComparison.OrdinalIgnoreCase) && x.RemainingTurns > 0)
            .ToList();

        if (existingStatuses.Count == 0) return [];

        var events = new List<PendingBattleEvent>();

        foreach (var status in existingStatuses)
        {
            var remainingTicks = status.RemainingTurns;
            var tickDmg = status.Value;
            var totalRemaining = remainingTicks * tickDmg * status.Stacks;
            var rawDetonate = totalRemaining * multiplier;

            var targetDef = BattleStatCalculator.GetEffectiveStat(context.Target, "DEF");
            var effectiveDef = armorIgnore > 0m
                ? Math.Max(0m, targetDef * (100m - armorIgnore) / 100m)
                : targetDef;

            var detonateDmg = Math.Max(1, (int)Math.Round(rawDetonate * 100m / (100m + effectiveDef)));

            var isCrit = false;
            if (canCrit && detonateDmg > 0)
            {
                isCrit = (decimal)context.Random.NextDouble() * 100m < context.Actor.CritChance;
                if (isCrit) detonateDmg = Math.Max(1, (int)Math.Round(detonateDmg * context.Actor.CritDamage / 100m));
            }

            if (removeStatus)
            {
                context.Target.StatusEffects.Remove(status);
            }

            var hpBefore = context.Target.Hp;
            context.Target.Hp = Math.Max(0, hpBefore - detonateDmg);
            var actualDamage = hpBefore - context.Target.Hp;

            events.Add(new PendingBattleEvent
            {
                EventType = BattleCodes.BleedDetonated,
                ActorId = context.Actor.Id,
                TargetId = context.Target.Id,
                SkillId = context.Skill.Id,
                EffectTypeCode = requiredStatus,
                DamageSchoolCode = school,
                Value = actualDamage,
                HpBefore = hpBefore,
                HpAfter = context.Target.Hp,
                IsCrit = isCrit
            });

            if (hpBefore > 0 && context.Target.Hp == 0)
            {
                events.Add(new PendingBattleEvent
                {
                    EventType = "DEATH",
                    ActorId = context.Actor.Id,
                    TargetId = context.Target.Id,
                    SkillId = context.Skill.Id
                });
                break;
            }
        }

        return events;
    }
}
