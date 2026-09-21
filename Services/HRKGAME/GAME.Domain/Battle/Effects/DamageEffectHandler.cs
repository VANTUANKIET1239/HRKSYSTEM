namespace GAME.Domain.Battle.Effects;

public sealed class DamageEffectHandler : IBattleEffectHandler
{
    public string EffectTypeCode => BattleCodes.Damage;

    public IReadOnlyList<PendingBattleEvent> Apply(BattleEffectContext context)
    {
        var effect = context.Effect;
        var actor = context.Actor;
        var target = context.Target;
        var rawDamage = BattleStatCalculator.CalculateEffectValue(effect, actor);
        var school = effect.DamageSchoolCode ?? BattleCodes.Physical;
        var defense = school.Equals(BattleCodes.Magic, StringComparison.OrdinalIgnoreCase)
            ? BattleStatCalculator.GetEffectiveStat(target, "MAGIC_RESISTANCE")
            : BattleStatCalculator.GetEffectiveStat(target, "DEF");
        var damage = school.Equals(BattleCodes.True, StringComparison.OrdinalIgnoreCase)
            ? Math.Max(1, (int)Math.Round(rawDamage))
            : Math.Max(1, (int)Math.Round(rawDamage * 100m / (100m + Math.Max(0m, defense))));
        var isCrit = (decimal)context.Random.NextDouble() * 100m < actor.CritChance;
        if (isCrit) damage = Math.Max(1, (int)Math.Round(damage * actor.CritDamage / 100m));

        var markPercent = SumStatusPercent(target, BattleCodes.Mark);
        var reductionPercent = Math.Clamp(SumStatusPercent(target, BattleCodes.DamageReduction), 0m, 90m);
        damage = Math.Max(1, (int)Math.Round(damage * (100m + markPercent) / 100m * (100m - reductionPercent) / 100m));

        var events = new List<PendingBattleEvent>();
        var remainingDamage = damage;
        foreach (var shield in target.StatusEffects.Where(x =>
                     x.EffectTypeCode.Equals(BattleCodes.Shield, StringComparison.OrdinalIgnoreCase) && x.ShieldRemaining > 0).ToList())
        {
            var absorbed = Math.Min(remainingDamage, shield.ShieldRemaining);
            shield.ShieldRemaining -= absorbed;
            remainingDamage -= absorbed;
            events.Add(new PendingBattleEvent { EventType = "SHIELD_ABSORBED", ActorId = actor.Id,
                TargetId = target.Id, SkillId = context.Skill.Id, EffectTypeCode = BattleCodes.Shield, Value = absorbed });
            if (shield.ShieldRemaining == 0)
            {
                target.StatusEffects.Remove(shield);
                events.Add(new PendingBattleEvent { EventType = "STATUS_EXPIRED", ActorId = shield.SourceHeroId,
                    TargetId = target.Id, SkillId = shield.SourceSkillId, EffectTypeCode = BattleCodes.Shield });
            }
            if (remainingDamage == 0) break;
        }

        var hpBefore = target.Hp;
        target.Hp = Math.Max(0, target.Hp - remainingDamage);
        events.Add(new PendingBattleEvent { EventType = "DAMAGE", ActorId = actor.Id, TargetId = target.Id,
            SkillId = context.Skill.Id, EffectTypeCode = effect.EffectTypeCode, DamageSchoolCode = school,
            Value = remainingDamage, HpBefore = hpBefore, HpAfter = target.Hp, IsCrit = isCrit });
        if (hpBefore > 0 && target.Hp == 0)
            events.Add(new PendingBattleEvent { EventType = "DEATH", ActorId = actor.Id,
                TargetId = target.Id, SkillId = context.Skill.Id });

        if (remainingDamage > 0 && actor.Id != target.Id && actor.IsAlive)
        {
            var reflectionPercent = SumStatusPercent(target, BattleCodes.DamageReflection);
            if (reflectionPercent > 0)
            {
                var reflected = Math.Max(1, (int)Math.Round(remainingDamage * reflectionPercent / 100m));
                var actorHpBefore = actor.Hp;
                actor.Hp = Math.Max(0, actor.Hp - reflected);
                events.Add(new PendingBattleEvent { EventType = "DAMAGE", ActorId = target.Id, TargetId = actor.Id,
                    SkillId = context.Skill.Id, EffectTypeCode = BattleCodes.DamageReflection,
                    DamageSchoolCode = BattleCodes.True, Value = reflected, HpBefore = actorHpBefore, HpAfter = actor.Hp });
                if (actorHpBefore > 0 && actor.Hp == 0)
                    events.Add(new PendingBattleEvent { EventType = "DEATH", ActorId = target.Id, TargetId = actor.Id,
                        SkillId = context.Skill.Id, EffectTypeCode = BattleCodes.DamageReflection });
            }
        }
        return events;
    }

    private static decimal SumStatusPercent(BattleCombatant target, string effectCode) =>
        target.StatusEffects.Where(x => x.RemainingTurns > 0 &&
                x.EffectTypeCode.Equals(effectCode, StringComparison.OrdinalIgnoreCase))
            .Sum(x => x.Value * x.Stacks);
}
