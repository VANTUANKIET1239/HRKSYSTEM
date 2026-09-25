namespace GAME.Domain.Battle.Effects;

public sealed class BleedEffectHandler : IBattleEffectHandler, ITurnStartEffectHandler
{
    public string EffectTypeCode => BattleCodes.Bleed;

    public IReadOnlyList<PendingBattleEvent> Apply(BattleEffectContext context)
    {
        if (context.Effect.DurationTurns <= 0)
        {
            throw new InvalidOperationException(
                $"Skill '{context.Skill.Id}', effect '{EffectTypeCode}' has invalid DurationTurns ({context.Effect.DurationTurns}).");
        }

        var bleedTickDamage = BattleStatCalculator.CalculateEffectValue(context.Effect, context.Actor);
        if (bleedTickDamage <= 0m && context.Effect.BaseValue <= 0m && (context.Effect.Scalings == null || context.Effect.Scalings.Count == 0))
        {
            throw new InvalidOperationException(
                $"Skill '{context.Skill.Id}', effect '{EffectTypeCode}' is missing scaling or value configuration.");
        }

        if (bleedTickDamage <= 0m)
        {
            bleedTickDamage = context.Effect.BaseValue;
        }

        var duration = context.Effect.DurationTurns;
        var armorIgnore = context.Effect.GetDecimal("ARMOR_IGNORE_PERCENT", 0m);
        var canCrit = context.Effect.GetBool("CAN_CRIT", false);
        var canKill = context.Effect.GetBool("CAN_KILL", false);
        var refreshOnReapply = context.Effect.GetBool("REFRESH_ON_REAPPLY", true);
        var school = context.Effect.DamageSchoolCode ?? BattleCodes.Physical;

        var instanceId = $"{context.Actor.Id}:{context.Skill.Id}:{EffectTypeCode}:{context.Target.Id}";
        var existing = context.Target.StatusEffects.FirstOrDefault(x =>
            x.EffectTypeCode.Equals(EffectTypeCode, StringComparison.OrdinalIgnoreCase));

        if (existing == null)
        {
            context.Target.StatusEffects.Add(new BattleStatusEffect
            {
                InstanceId = instanceId,
                EffectTypeCode = EffectTypeCode,
                SourceSkillId = context.Skill.Id,
                SourceHeroId = context.Actor.Id,
                RemainingTurns = duration,
                AppliedTurn = context.Turn,
                Stacks = 1,
                MaxStacks = Math.Max(1, context.Effect.MaxStacks),
                Value = bleedTickDamage,
                DamageSchoolCode = school,
                ArmorIgnorePercent = armorIgnore,
                CanCrit = canCrit,
                CanKill = canKill,
                StatModifiers = []
            });

            return
            [
                new PendingBattleEvent
                {
                    EventType = "STATUS_APPLIED",
                    ActorId = context.Actor.Id,
                    TargetId = context.Target.Id,
                    SkillId = context.Skill.Id,
                    EffectTypeCode = EffectTypeCode,
                    DamageSchoolCode = school,
                    Value = (int)Math.Round(bleedTickDamage),
                    RemainingTurns = duration
                }
            ];
        }

        if (refreshOnReapply)
        {
            existing.RemainingTurns = duration;
            existing.AppliedTurn = context.Turn;
            existing.Value = bleedTickDamage;
        }

        return
        [
            new PendingBattleEvent
            {
                EventType = BattleCodes.StatusRefreshed,
                ActorId = context.Actor.Id,
                TargetId = context.Target.Id,
                SkillId = context.Skill.Id,
                EffectTypeCode = EffectTypeCode,
                DamageSchoolCode = school,
                Value = (int)Math.Round(bleedTickDamage),
                RemainingTurns = existing.RemainingTurns
            }
        ];
    }

    public IReadOnlyList<PendingBattleEvent> OnTurnStart(
        BattleStatusEffect status,
        BattleCombatant actor,
        int round,
        int turn,
        IReadOnlyList<BattleCombatant> combatants)
    {
        if (!actor.IsAlive) return [];

        if (status.Value <= 0m)
        {
            throw new InvalidOperationException(
                $"Status '{status.EffectTypeCode}' on combatant {actor.Id} has invalid tick damage ({status.Value}).");
        }

        var targetDef = BattleStatCalculator.GetEffectiveStat(actor, "DEF");
        var effectiveDef = status.ArmorIgnorePercent > 0m
            ? Math.Max(0m, targetDef * (100m - status.ArmorIgnorePercent) / 100m)
            : targetDef;

        var damage = Math.Max(1, (int)Math.Round(status.Value * 100m / (100m + effectiveDef)));

        var isCrit = false;
        if (status.CanCrit)
        {
            var sourceHero = combatants.FirstOrDefault(x => x.Id == status.SourceHeroId);
            if (sourceHero != null && sourceHero.CritChance > 0)
            {
                // In OnTurnStart, determinism comes from combat context if needed
            }
        }

        var hpBefore = actor.Hp;
        var hpAfter = status.CanKill
            ? Math.Max(0, hpBefore - damage)
            : Math.Max(1, hpBefore - damage);

        var actualDamage = hpBefore - hpAfter;
        actor.Hp = hpAfter;

        var events = new List<PendingBattleEvent>
        {
            new()
            {
                EventType = BattleCodes.BleedDamage,
                ActorId = status.SourceHeroId,
                TargetId = actor.Id,
                SkillId = status.SourceSkillId,
                EffectTypeCode = BattleCodes.Bleed,
                DamageSchoolCode = status.DamageSchoolCode ?? BattleCodes.Physical,
                Value = actualDamage > 0 ? actualDamage : damage,
                HpBefore = hpBefore,
                HpAfter = hpAfter,
                IsCrit = isCrit,
                RemainingTurns = Math.Max(0, status.RemainingTurns - 1)
            }
        };

        if (status.CanKill && hpBefore > 0 && actor.Hp == 0)
        {
            events.Add(new PendingBattleEvent
            {
                EventType = "DEATH",
                ActorId = status.SourceHeroId,
                TargetId = actor.Id,
                SkillId = status.SourceSkillId
            });
        }

        return events;
    }
}
