using GAME.Domain.Battle.Effects;
using GAME.Domain.Battle.Targets;

namespace GAME.Domain.Battle.Skills.HaiLastSmile;

public sealed class HaiLastSmileSkillHandler : ISkillHandler
{
    private readonly DefaultSkillHandler _defaultHandler;
    private readonly BattleEffectHandlerRegistry _effectHandlers;
    private readonly DamageEffectHandler _damageHandler;

    public HaiLastSmileSkillHandler(
        DefaultSkillHandler defaultHandler,
        BattleEffectHandlerRegistry effectHandlers,
        BattleTargetSelectorRegistry? targetSelectors = null)
    {
        _defaultHandler = defaultHandler;
        _effectHandlers = effectHandlers;
        _damageHandler = (effectHandlers.GetRequired(BattleCodes.Damage) as DamageEffectHandler) ?? new DamageEffectHandler();
    }

    public static HaiLastSmileSkillHandler Create(DefaultSkillHandler defaultHandler) =>
        new(defaultHandler, BattleEffectHandlerRegistry.CreateDefault());

    public bool CanHandle(BattleSkill skill) =>
        skill.Id.Equals(HaiLastSmileSkillCodes.Basic, StringComparison.OrdinalIgnoreCase) ||
        skill.Id.Equals(HaiLastSmileSkillCodes.Ultimate, StringComparison.OrdinalIgnoreCase);

    public SkillExecutionResult Execute(SkillExecutionContext context) =>
        context.Skill.Id.Equals(HaiLastSmileSkillCodes.Basic, StringComparison.OrdinalIgnoreCase)
            ? ExecuteBasic(context)
            : ExecuteUltimate(context);

    private SkillExecutionResult ExecuteBasic(SkillExecutionContext context)
    {
        var result = new SkillExecutionResult();

        var damageEffect = context.Skill.Effects.FirstOrDefault(x =>
            x.EffectTypeCode.Equals(BattleCodes.Damage, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"Skill '{context.Skill.Id}' is missing a '{BattleCodes.Damage}' effect.");

        var target = _defaultHandler.ResolveTargets(damageEffect.TargetTypeCode, context).FirstOrDefault();
        if (target == null) return result;

        var effectContext = new BattleEffectContext
        {
            Effect = damageEffect,
            Skill = context.Skill,
            Actor = context.Actor,
            Target = target,
            SelectedTargets = [target],
            Combatants = context.Combatants,
            Random = context.Random,
            Round = context.Round,
            Turn = context.Turn
        };

        var damageResult = _damageHandler.ExecuteDamage(effectContext);
        foreach (var evt in damageResult.EmittedEvents)
        {
            result.Events.Add(Stamp(evt, "SLASH", 1, 450, "IMPACT"));
        }

        if (damageResult.WasHit)
        {
            result.BasicAttackHitTargetIds.Add(target.Id);
        }
        if (damageResult.WasKilled && !result.DefeatedTargetIds.Contains(target.Id))
        {
            result.DefeatedTargetIds.Add(target.Id);
        }

        if (damageResult.WasHit && target.IsAlive)
        {
            var riderEffects = context.Skill.Effects
                .Where(x => !x.EffectTypeCode.Equals(BattleCodes.Damage, StringComparison.OrdinalIgnoreCase)
                            && string.IsNullOrEmpty(x.ConditionCode))
                .OrderBy(x => x.DisplayOrder);

            foreach (var effect in riderEffects)
            {
                var roll = (decimal)context.Random.NextDouble() * 100m;
                if (roll <= effect.ChancePercent)
                {
                    var handler = _effectHandlers.GetRequired(effect.EffectTypeCode);
                    var effectEvents = handler.Apply(new BattleEffectContext
                    {
                        Effect = effect,
                        Skill = context.Skill,
                        Actor = context.Actor,
                        Target = target,
                        SelectedTargets = [target],
                        Combatants = context.Combatants,
                        Random = context.Random,
                        Round = context.Round,
                        Turn = context.Turn
                    });
                    foreach (var evt in effectEvents)
                    {
                        result.Events.Add(Stamp(evt, "SLASH", 1, 500, "IMPACT"));
                    }
                }
            }
        }

        return result;
    }

    private SkillExecutionResult ExecuteUltimate(SkillExecutionContext context)
    {
        var result = new SkillExecutionResult();

        var primaryTargetCode = context.Skill.Effects
            .FirstOrDefault(x => x.EffectTypeCode.Equals(BattleCodes.Damage, StringComparison.OrdinalIgnoreCase))?.TargetTypeCode
            ?? context.Skill.Effects.FirstOrDefault()?.TargetTypeCode
            ?? BattleCodes.LowestHpPercent;

        var target = _defaultHandler.ResolveTargets(primaryTargetCode, context).FirstOrDefault();
        if (target == null) return result;

        var detonateEffect = context.Skill.Effects.FirstOrDefault(x =>
            x.EffectTypeCode.Equals(BattleCodes.BleedDetonate, StringComparison.OrdinalIgnoreCase));
        var requiredStatus = detonateEffect?.GetString("REQUIRED_STATUS_CODE", BattleCodes.Bleed) ?? BattleCodes.Bleed;
        var hasPreexistingStatus = target.StatusEffects.Any(x =>
            x.EffectTypeCode.Equals(requiredStatus, StringComparison.OrdinalIgnoreCase) && x.RemainingTurns > 0);

        // 1. Setup Effects (e.g. PANIC, BLEED_DETONATE)
        var setupEffects = context.Skill.Effects
            .Where(x => string.IsNullOrEmpty(x.ExecutionGroup) && string.IsNullOrEmpty(x.ConditionCode))
            .OrderBy(x => x.DisplayOrder);

        foreach (var effect in setupEffects)
        {
            if (!target.IsAlive) break;

            if (effect.EffectTypeCode.Equals(BattleCodes.BleedDetonate, StringComparison.OrdinalIgnoreCase) && !hasPreexistingStatus)
            {
                continue;
            }

            var isPanic = effect.EffectTypeCode.Equals(BattleCodes.Panic, StringComparison.OrdinalIgnoreCase);
            var setupOffset = isPanic ? 400 : 700;
            var setupPhase = isPanic ? "PREPARE" : "DETONATE";

            var handler = _effectHandlers.GetRequired(effect.EffectTypeCode);
            var effectEvents = handler.Apply(new BattleEffectContext
            {
                Effect = effect,
                Skill = context.Skill,
                Actor = context.Actor,
                Target = target,
                SelectedTargets = [target],
                Combatants = context.Combatants,
                Random = context.Random,
                Round = context.Round,
                Turn = context.Turn
            });
            foreach (var evt in effectEvents)
            {
                result.Events.Add(Stamp(evt, "SETUP", null, setupOffset, setupPhase));
            }

            if (!target.IsAlive && !result.DefeatedTargetIds.Contains(target.Id))
            {
                result.DefeatedTargetIds.Add(target.Id);
                break;
            }
        }

        // 2. Sequential Strike Groups (e.g. HIT_1, HIT_2, HIT_3)
        var executionGroups = context.Skill.Effects
            .Where(x => !string.IsNullOrEmpty(x.ExecutionGroup) && string.IsNullOrEmpty(x.ConditionCode))
            .GroupBy(x => x.ExecutionGroup!, StringComparer.OrdinalIgnoreCase)
            .OrderBy(g => g.Min(x => x.DisplayOrder));

        var hitCounter = 0;
        foreach (var group in executionGroups)
        {
            if (!target.IsAlive) break;
            hitCounter++;

            // Hit timings synchronized with animation: Hit 1 -> 1050ms, Hit 2 -> 1400ms, Hit 3 -> 1900ms
            var (hitOffset, riderOffset, phase) = hitCounter switch
            {
                1 => (1050, 1120, "HIT_1"),
                2 => (1400, 1470, "HIT_2"),
                3 => (1900, 1980, "HIT_3"),
                _ => (1900 + (hitCounter - 3) * 450, 1980 + (hitCounter - 3) * 450, $"HIT_{hitCounter}")
            };

            var damageEffect = group.FirstOrDefault(x => x.EffectTypeCode.Equals(BattleCodes.Damage, StringComparison.OrdinalIgnoreCase));
            if (damageEffect != null)
            {
                var hitResult = _damageHandler.ExecuteDamage(new BattleEffectContext
                {
                    Effect = damageEffect,
                    Skill = context.Skill,
                    Actor = context.Actor,
                    Target = target,
                    SelectedTargets = [target],
                    Combatants = context.Combatants,
                    Random = context.Random,
                    Round = context.Round,
                    Turn = context.Turn
                });
                foreach (var evt in hitResult.EmittedEvents)
                {
                    result.Events.Add(Stamp(evt, group.Key, hitCounter, hitOffset, phase));
                }

                if (hitResult.WasKilled && !result.DefeatedTargetIds.Contains(target.Id))
                {
                    result.DefeatedTargetIds.Add(target.Id);
                }

                if (hitResult.WasHit && target.IsAlive)
                {
                    var riderEffects = group.Where(x => !x.EffectTypeCode.Equals(BattleCodes.Damage, StringComparison.OrdinalIgnoreCase))
                        .OrderBy(x => x.DisplayOrder);

                    foreach (var rider in riderEffects)
                    {
                        var roll = (decimal)context.Random.NextDouble() * 100m;
                        if (roll <= rider.ChancePercent)
                        {
                            var handler = _effectHandlers.GetRequired(rider.EffectTypeCode);
                            var riderEvents = handler.Apply(new BattleEffectContext
                            {
                                Effect = rider,
                                Skill = context.Skill,
                                Actor = context.Actor,
                                Target = target,
                                SelectedTargets = [target],
                                Combatants = context.Combatants,
                                Random = context.Random,
                                Round = context.Round,
                                Turn = context.Turn
                            });
                            foreach (var evt in riderEvents)
                            {
                                result.Events.Add(Stamp(evt, group.Key, hitCounter, riderOffset, phase));
                            }
                        }
                    }
                }
            }
        }

        // 3. Conditional Outcome Evaluation (TARGET_DEFEATED vs TARGET_SURVIVED)
        var conditionCode = !target.IsAlive ? "TARGET_DEFEATED" : "TARGET_SURVIVED";
        var conditionalEffects = context.Skill.Effects
            .Where(x => !string.IsNullOrEmpty(x.ConditionCode) &&
                        x.ConditionCode.Equals(conditionCode, StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => x.DisplayOrder);

        foreach (var effect in conditionalEffects)
        {
            var effectTarget = effect.TargetTypeCode.Equals(BattleCodes.Self, StringComparison.OrdinalIgnoreCase)
                ? context.Actor
                : _defaultHandler.ResolveTargets(effect.TargetTypeCode, context).FirstOrDefault();

            if (effectTarget == null) continue;

            var handler = _effectHandlers.GetRequired(effect.EffectTypeCode);
            var effectEvents = handler.Apply(new BattleEffectContext
            {
                Effect = effect,
                Skill = context.Skill,
                Actor = context.Actor,
                Target = effectTarget,
                SelectedTargets = [effectTarget],
                Combatants = context.Combatants,
                Random = context.Random,
                Round = context.Round,
                Turn = context.Turn
            });
            foreach (var evt in effectEvents)
            {
                result.Events.Add(Stamp(evt, "OUTCOME", null, 2200, "OUTCOME"));
            }
        }

        return result;
    }

    private static PendingBattleEvent Stamp(
        PendingBattleEvent evt,
        string? executionGroup,
        int? hitIndex,
        int timelineOffsetMs,
        string? phaseCode) =>
        new()
        {
            EventType = evt.EventType,
            ActorId = evt.ActorId,
            TargetId = evt.TargetId,
            SkillId = evt.SkillId,
            EffectTypeCode = evt.EffectTypeCode,
            DamageSchoolCode = evt.DamageSchoolCode,
            Value = evt.Value,
            HpBefore = evt.HpBefore,
            HpAfter = evt.HpAfter,
            EnergyBefore = evt.EnergyBefore,
            EnergyAfter = evt.EnergyAfter,
            IsCrit = evt.IsCrit,
            RemainingTurns = evt.RemainingTurns,
            StatModifiers = evt.StatModifiers,
            ExecutionGroup = executionGroup,
            HitIndex = hitIndex,
            TimelineOffsetMs = timelineOffsetMs,
            PhaseCode = phaseCode
        };
}
