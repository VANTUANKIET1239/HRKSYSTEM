using GAME.Domain.Battle.Effects;
using GAME.Domain.Battle.Targets;

namespace GAME.Domain.Battle.Skills.TruongKietGraduation;

public sealed class TruongKietGraduationSkillHandler : ISkillHandler
{
    private readonly DefaultSkillHandler _defaultHandler;
    private readonly DamageEffectHandler _damageHandler;

    public TruongKietGraduationSkillHandler(
        DefaultSkillHandler defaultHandler,
        BattleEffectHandlerRegistry effectHandlers)
    {
        _defaultHandler = defaultHandler;
        _damageHandler = (effectHandlers.GetRequired(BattleCodes.Damage) as DamageEffectHandler) ?? new DamageEffectHandler();
    }

    public static TruongKietGraduationSkillHandler Create(DefaultSkillHandler defaultHandler) =>
        new(defaultHandler, BattleEffectHandlerRegistry.CreateDefault());

    public bool CanHandle(BattleSkill skill) =>
        skill.Id.Equals(TruongKietGraduationSkillCodes.Basic, StringComparison.OrdinalIgnoreCase) ||
        skill.Id.Equals(TruongKietGraduationSkillCodes.Energy, StringComparison.OrdinalIgnoreCase);

    public SkillExecutionResult Execute(SkillExecutionContext context) =>
        context.Skill.Id.Equals(TruongKietGraduationSkillCodes.Basic, StringComparison.OrdinalIgnoreCase)
            ? ExecuteBasic(context)
            : ExecuteEnergy(context);

    private SkillExecutionResult ExecuteBasic(SkillExecutionContext context)
    {
        var result = new SkillExecutionResult();

        // 1. Ensure Tin Chi listener status exists on Truong Kiet
        EnsureTinChiStatus(context.Actor, context.Skill.Id, context.Turn);

        var effects = context.Skill.Effects.OrderBy(x => x.DisplayOrder).ToList();
        var primaryEffect = effects.FirstOrDefault(x => x.EffectTypeCode.Equals(BattleCodes.Damage, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"Skill '{context.Skill.Id}' has no Damage effect configured.");

        // Target: Enemy Single
        var target = _defaultHandler.ResolveTargets(primaryEffect.TargetTypeCode, context, primaryEffect).FirstOrDefault();
        if (target != null)
        {
            result.TargetedCombatantIds.Add(target.Id);

            var dmgContext = new BattleEffectContext
            {
                Effect = primaryEffect,
                Skill = context.Skill,
                Actor = context.Actor,
                Target = target,
                SelectedTargets = [target],
                Combatants = context.Combatants,
                Random = context.Random,
                Round = context.Round,
                Turn = context.Turn,
                ActionId = context.ActionId,
                TimelineOffsetMs = 800,
                PhaseCode = "IMPACT"
            };

            var dmgResult = _damageHandler.ExecuteDamage(dmgContext);
            foreach (var evt in dmgResult.EmittedEvents)
            {
                result.Events.Add(evt);
            }

            if (dmgResult.WasHit)
            {
                result.BasicAttackHitTargetIds.Add(target.Id);
            }
            if (dmgResult.WasKilled)
            {
                result.DefeatedTargetIds.Add(target.Id);
            }
        }

        // 2. Shield lowest HP% ally
        var currentStacks = context.Actor.GetResource(BattleCodes.TinChiDanhDu, 0);
        var baseShieldPercent = primaryEffect.GetDecimal("BASE_SHIELD_MAX_HP_PERCENT", 0.08m);
        var empoweredShieldPercent = primaryEffect.GetDecimal("EMPOWERED_SHIELD_MAX_HP_PERCENT", 0.11m);

        var shieldPercent = currentStacks >= 3 ? empoweredShieldPercent : baseShieldPercent;
        var shieldValue = Math.Max(1, (int)Math.Round(context.Actor.MaxHp * shieldPercent));

        var lowestAlly = context.Combatants
            .Where(x => x.Team == context.Actor.Team && x.IsAlive)
            .OrderBy(x => x.MaxHp > 0 ? (decimal)x.Hp / x.MaxHp : 0m)
            .ThenBy(x => x.Position)
            .ThenBy(x => x.Id)
            .FirstOrDefault();

        if (lowestAlly != null)
        {
            ApplyShield(lowestAlly, context.Actor, context.Skill.Id, shieldValue, 2, context.Turn, result.Events);
        }

        return result;
    }

    private SkillExecutionResult ExecuteEnergy(SkillExecutionContext context)
    {
        var result = new SkillExecutionResult();

        // 1. Ensure listener exists
        EnsureTinChiStatus(context.Actor, context.Skill.Id, context.Turn);

        var effects = context.Skill.Effects.OrderBy(x => x.DisplayOrder).ToList();
        var primaryEffect = effects.FirstOrDefault(x => x.EffectTypeCode.Equals(BattleCodes.Damage, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"Skill '{context.Skill.Id}' has no Damage effect configured.");

        // 2. Consume all Tin Chi
        var stacksConsumed = context.Actor.GetResource(BattleCodes.TinChiDanhDu, 0);
        context.Actor.SetResource(BattleCodes.TinChiDanhDu, 0);

        var status = context.Actor.StatusEffects.FirstOrDefault(s =>
            s.EffectTypeCode.Equals(BattleCodes.TinChiDanhDu, StringComparison.OrdinalIgnoreCase));
        if (status != null)
        {
            status.Stacks = 0;
        }

        if (stacksConsumed > 0)
        {
            result.Events.Add(new PendingBattleEvent
            {
                EventType = BattleCodes.StatusStackChanged,
                ActorId = context.Actor.Id,
                TargetId = context.Actor.Id,
                SkillId = context.Skill.Id,
                EffectTypeCode = BattleCodes.TinChiDanhDu,
                PreviousStacks = stacksConsumed,
                CurrentStacks = 0,
                ResourceCode = BattleCodes.TinChiDanhDu,
                PreviousValue = stacksConsumed,
                CurrentValue = 0,
                TimelineOffsetMs = 200,
                PhaseCode = "CAST"
            });
        }

        // 3. Front row AoE damage
        var targets = _defaultHandler.ResolveTargets(primaryEffect.TargetTypeCode, context, primaryEffect);
        foreach (var t in targets)
        {
            result.TargetedCombatantIds.Add(t.Id);
            if (!t.IsAlive) continue;

            var dmgContext = new BattleEffectContext
            {
                Effect = primaryEffect,
                Skill = context.Skill,
                Actor = context.Actor,
                Target = t,
                SelectedTargets = targets,
                Combatants = context.Combatants,
                Random = context.Random,
                Round = context.Round,
                Turn = context.Turn,
                ActionId = context.ActionId,
                TimelineOffsetMs = 800,
                PhaseCode = "IMPACT"
            };

            var dmgResult = _damageHandler.ExecuteDamage(dmgContext);
            foreach (var evt in dmgResult.EmittedEvents)
            {
                result.Events.Add(evt);
            }

            if (dmgResult.WasKilled)
            {
                result.DefeatedTargetIds.Add(t.Id);
            }
        }

        // 4. Team shield: 9% Max HP + 2% Max HP per consumed stack, 2 turns
        var baseTeamShieldPercent = primaryEffect.GetDecimal("BASE_TEAM_SHIELD_PERCENT", 0.09m);
        var shieldPerStackPercent = primaryEffect.GetDecimal("SHIELD_PER_STACK_PERCENT", 0.02m);
        var totalShieldPercent = baseTeamShieldPercent + (stacksConsumed * shieldPerStackPercent);
        var teamShieldValue = Math.Max(1, (int)Math.Round(context.Actor.MaxHp * totalShieldPercent));

        var livingAllies = context.Combatants.Where(x => x.Team == context.Actor.Team && x.IsAlive).ToList();
        foreach (var ally in livingAllies)
        {
            ApplyShield(ally, context.Actor, context.Skill.Id, teamShieldValue, 2, context.Turn, result.Events);
        }

        // 5. If consumed full 3 stacks: 20% Damage Reduction for 2 turns on Truong Kiet
        if (stacksConsumed >= 3)
        {
            var drPercent = primaryEffect.GetDecimal("DAMAGE_REDUCTION_PERCENT", 20m);
            var existingDr = context.Actor.StatusEffects.FirstOrDefault(s =>
                s.EffectTypeCode.Equals(BattleCodes.DamageReduction, StringComparison.OrdinalIgnoreCase) &&
                s.SourceHeroId == context.Actor.Id);

            if (existingDr != null)
            {
                existingDr.RemainingTurns = 2;
                existingDr.Value = drPercent;
                result.Events.Add(new PendingBattleEvent
                {
                    EventType = BattleCodes.StatusRefreshed,
                    ActorId = context.Actor.Id,
                    TargetId = context.Actor.Id,
                    SkillId = context.Skill.Id,
                    EffectTypeCode = BattleCodes.DamageReduction,
                    Value = (int)drPercent,
                    RemainingTurns = 2,
                    TimelineOffsetMs = 1200,
                    PhaseCode = "RECOVERY"
                });
            }
            else
            {
                var instanceId = $"{context.Actor.Id}:{context.Skill.Id}:{BattleCodes.DamageReduction}:{context.Actor.Id}";
                context.Actor.StatusEffects.Add(new BattleStatusEffect
                {
                    InstanceId = instanceId,
                    EffectTypeCode = BattleCodes.DamageReduction,
                    SourceSkillId = context.Skill.Id,
                    SourceHeroId = context.Actor.Id,
                    RemainingTurns = 2,
                    AppliedTurn = context.Turn,
                    Stacks = 1,
                    MaxStacks = 1,
                    Value = drPercent,
                    Dispellable = true
                });

                result.Events.Add(new PendingBattleEvent
                {
                    EventType = "STATUS_APPLIED",
                    ActorId = context.Actor.Id,
                    TargetId = context.Actor.Id,
                    SkillId = context.Skill.Id,
                    EffectTypeCode = BattleCodes.DamageReduction,
                    Value = (int)drPercent,
                    RemainingTurns = 2,
                    TimelineOffsetMs = 1200,
                    PhaseCode = "RECOVERY"
                });
            }
        }

        return result;
    }

    private static void EnsureTinChiStatus(BattleCombatant actor, string skillId, int turn)
    {
        var existing = actor.StatusEffects.FirstOrDefault(s =>
            s.EffectTypeCode.Equals(BattleCodes.TinChiDanhDu, StringComparison.OrdinalIgnoreCase));

        if (existing == null)
        {
            var instanceId = $"{actor.Id}:{skillId}:{BattleCodes.TinChiDanhDu}:{actor.Id}";
            actor.StatusEffects.Add(new BattleStatusEffect
            {
                InstanceId = instanceId,
                EffectTypeCode = BattleCodes.TinChiDanhDu,
                SourceSkillId = skillId,
                SourceHeroId = actor.Id,
                RemainingTurns = -1,
                AppliedTurn = turn,
                Stacks = 0,
                MaxStacks = 3,
                Dispellable = false,
                ScaleModifiersWithStacks = true,
                StatModifiers =
                [
                    new BattleStatModifier("DEF", "PERCENT", 6m, "Giáp"),
                    new BattleStatModifier("MAGIC_RESISTANCE", "PERCENT", 6m, "Kháng Phép")
                ]
            });
        }
    }

    private static void ApplyShield(
        BattleCombatant target,
        BattleCombatant actor,
        string skillId,
        int shieldValue,
        int duration,
        int turn,
        List<PendingBattleEvent> events)
    {
        var instanceId = $"{actor.Id}:{skillId}:{BattleCodes.Shield}:{target.Id}";
        var existing = target.StatusEffects.FirstOrDefault(x => x.InstanceId == instanceId);

        if (existing == null)
        {
            target.StatusEffects.Add(new BattleStatusEffect
            {
                InstanceId = instanceId,
                EffectTypeCode = BattleCodes.Shield,
                SourceSkillId = skillId,
                SourceHeroId = actor.Id,
                RemainingTurns = duration,
                AppliedTurn = turn,
                Stacks = 1,
                MaxStacks = 1,
                Value = shieldValue,
                ShieldRemaining = shieldValue,
                Dispellable = true
            });
        }
        else
        {
            existing.RemainingTurns = duration;
            existing.AppliedTurn = turn;
            existing.ShieldRemaining = Math.Max(existing.ShieldRemaining, shieldValue);
            existing.Value = existing.ShieldRemaining;
        }

        events.Add(new PendingBattleEvent
        {
            EventType = "STATUS_APPLIED",
            ActorId = actor.Id,
            TargetId = target.Id,
            SkillId = skillId,
            EffectTypeCode = BattleCodes.Shield,
            Value = shieldValue,
            RemainingTurns = duration,
            TimelineOffsetMs = 1000,
            PhaseCode = "IMPACT"
        });
    }
}
