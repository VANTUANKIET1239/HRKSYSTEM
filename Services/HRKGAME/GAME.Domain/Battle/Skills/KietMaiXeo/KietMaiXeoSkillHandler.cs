using GAME.Domain.Battle.Effects;
using GAME.Domain.Battle.Targets;

namespace GAME.Domain.Battle.Skills.KietMaiXeo;

public sealed class KietMaiXeoSkillHandler : ISkillHandler
{
    private readonly DefaultSkillHandler _defaultHandler;
    private readonly DamageEffectHandler _damageHandler;

    public KietMaiXeoSkillHandler(
        DefaultSkillHandler defaultHandler,
        BattleEffectHandlerRegistry effectHandlers)
    {
        _defaultHandler = defaultHandler;
        _damageHandler = (effectHandlers.GetRequired(BattleCodes.Damage) as DamageEffectHandler) ?? new DamageEffectHandler();
    }

    public static KietMaiXeoSkillHandler Create(DefaultSkillHandler defaultHandler) =>
        new(defaultHandler, BattleEffectHandlerRegistry.CreateDefault());

    public bool CanHandle(BattleSkill skill) =>
        skill.Id.Equals(KietMaiXeoSkillCodes.Basic, StringComparison.OrdinalIgnoreCase) ||
        skill.Id.Equals(KietMaiXeoSkillCodes.Energy, StringComparison.OrdinalIgnoreCase);

    public SkillExecutionResult Execute(SkillExecutionContext context) =>
        context.Skill.Id.Equals(KietMaiXeoSkillCodes.Basic, StringComparison.OrdinalIgnoreCase)
            ? ExecuteBasic(context)
            : ExecuteEnergy(context);

    private SkillExecutionResult ExecuteBasic(SkillExecutionContext context)
    {
        var result = new SkillExecutionResult();
        var effects = context.Skill.Effects.OrderBy(x => x.DisplayOrder).ToList();
        var primaryEffect = effects.FirstOrDefault(x => x.EffectTypeCode.Equals(BattleCodes.Damage, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"Skill '{context.Skill.Id}' has no Damage effect configured.");

        var target = _defaultHandler.ResolveTargets(primaryEffect.TargetTypeCode, context, primaryEffect).FirstOrDefault();
        if (target == null) return result;

        result.TargetedCombatantIds.Add(target.Id);

        var backRowCritBonus = primaryEffect.GetDecimal("BACK_ROW_CRIT_BONUS_PERCENT", 15m);
        var isBackRow = target.Position is 2 or 4;
        var originalCrit = context.Actor.CritChance;

        if (isBackRow && backRowCritBonus > 0m)
        {
            context.Actor.CritChance += backRowCritBonus;
        }

        DamageEffectExecutionResult dmgResult;
        try
        {
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
            dmgResult = _damageHandler.ExecuteDamage(dmgContext);
        }
        finally
        {
            if (isBackRow && backRowCritBonus > 0m)
            {
                context.Actor.CritChance = originalCrit;
            }
        }

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

        // On direct critical hit: gain 1 Phong An (Max 3, max 1 per action)
        if (dmgResult.WasHit && dmgResult.WasCrit && context.Actor.IsAlive)
        {
            var currentStacks = context.Actor.GetResource(BattleCodes.PhongAn, 0);
            if (currentStacks < 3)
            {
                var newStacks = currentStacks + 1;
                context.Actor.SetResource(BattleCodes.PhongAn, newStacks);

                var existingStatus = context.Actor.StatusEffects.FirstOrDefault(s =>
                    s.EffectTypeCode.Equals(BattleCodes.PhongAn, StringComparison.OrdinalIgnoreCase));

                if (existingStatus == null)
                {
                    var status = new BattleStatusEffect
                    {
                        InstanceId = $"{context.Actor.Id}:{context.Skill.Id}:{BattleCodes.PhongAn}:{context.Actor.Id}",
                        EffectTypeCode = BattleCodes.PhongAn,
                        SourceSkillId = context.Skill.Id,
                        SourceHeroId = context.Actor.Id,
                        RemainingTurns = -1,
                        AppliedTurn = context.Turn,
                        Stacks = newStacks,
                        MaxStacks = 3,
                        Dispellable = false,
                        ScaleModifiersWithStacks = true,
                        StatModifiers = [new BattleStatModifier("SPD", "PERCENT", 5m, "Tốc Độ")]
                    };
                    context.Actor.StatusEffects.Add(status);

                    result.Events.Add(new PendingBattleEvent
                    {
                        EventType = "STATUS_APPLIED",
                        ActorId = context.Actor.Id,
                        TargetId = context.Actor.Id,
                        SkillId = context.Skill.Id,
                        EffectTypeCode = BattleCodes.PhongAn,
                        RemainingTurns = -1,
                        CurrentStacks = newStacks,
                        MaxStacks = 3,
                        StatModifiers = status.StatModifiers,
                        ResourceCode = BattleCodes.PhongAn,
                        CurrentValue = newStacks,
                        TimelineOffsetMs = 850,
                        PhaseCode = "IMPACT"
                    });
                }
                else
                {
                    existingStatus.Stacks = newStacks;

                    result.Events.Add(new PendingBattleEvent
                    {
                        EventType = BattleCodes.StatusStackChanged,
                        ActorId = context.Actor.Id,
                        TargetId = context.Actor.Id,
                        SkillId = context.Skill.Id,
                        EffectTypeCode = BattleCodes.PhongAn,
                        PreviousStacks = currentStacks,
                        CurrentStacks = newStacks,
                        MaxStacks = 3,
                        StatModifiers = existingStatus.StatModifiers,
                        ResourceCode = BattleCodes.PhongAn,
                        PreviousValue = currentStacks,
                        CurrentValue = newStacks,
                        TimelineOffsetMs = 850,
                        PhaseCode = "IMPACT"
                    });
                }
            }
        }

        return result;
    }

    private SkillExecutionResult ExecuteEnergy(SkillExecutionContext context)
    {
        var result = new SkillExecutionResult();
        var effects = context.Skill.Effects.OrderBy(x => x.DisplayOrder).ToList();
        var primaryEffect = effects.FirstOrDefault(x => x.EffectTypeCode.Equals(BattleCodes.Damage, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"Skill '{context.Skill.Id}' has no Damage effect configured.");

        // Target: Lowest HP percent, locked for all hits
        var target = _defaultHandler.ResolveTargets(primaryEffect.TargetTypeCode, context, primaryEffect).FirstOrDefault();
        if (target == null) return result;

        result.TargetedCombatantIds.Add(target.Id);

        // 1. Consume all Phong An at start of cast
        var stacksConsumed = context.Actor.GetResource(BattleCodes.PhongAn, 0);
        context.Actor.SetResource(BattleCodes.PhongAn, 0);

        var existingStatus = context.Actor.StatusEffects.FirstOrDefault(s =>
            s.EffectTypeCode.Equals(BattleCodes.PhongAn, StringComparison.OrdinalIgnoreCase));
        if (existingStatus != null)
        {
            context.Actor.StatusEffects.Remove(existingStatus);
        }

        if (stacksConsumed > 0)
        {
            result.Events.Add(new PendingBattleEvent
            {
                EventType = BattleCodes.StatusRemoved,
                ActorId = context.Actor.Id,
                TargetId = context.Actor.Id,
                SkillId = context.Skill.Id,
                EffectTypeCode = BattleCodes.PhongAn,
                PreviousStacks = stacksConsumed,
                CurrentStacks = 0,
                ResourceCode = BattleCodes.PhongAn,
                PreviousValue = stacksConsumed,
                CurrentValue = 0,
                TimelineOffsetMs = 200,
                PhaseCode = "CAST"
            });
        }

        var damageBonusPerStack = primaryEffect.GetDecimal("DAMAGE_BONUS_PER_STACK_PERCENT", 12m);
        var totalDamageBonus = stacksConsumed * damageBonusPerStack;
        var maxStackArmorIgnore = primaryEffect.GetDecimal("MAX_STACK_ARMOR_IGNORE_PERCENT", 20m);

        // 3 hits
        var hitCount = primaryEffect.GetInt("HIT_COUNT", 3);
        var hitOffsets = new[] { 400, 700, 1000 };

        for (var i = 1; i <= hitCount; i++)
        {
            if (!target.IsAlive) break;

            var armorIgnore = (i == hitCount && stacksConsumed >= 3) ? maxStackArmorIgnore : 0m;

            var hitParams = new Dictionary<string, BattleSkillEffectParameter>(primaryEffect.Parameters, StringComparer.OrdinalIgnoreCase)
            {
                ["DAMAGE_BONUS_PERCENT"] = new("DAMAGE_BONUS_PERCENT", totalDamageBonus, null, null, null),
                ["ARMOR_IGNORE_PERCENT"] = new("ARMOR_IGNORE_PERCENT", armorIgnore, null, null, null),
                ["CAN_CRIT"] = new("CAN_CRIT", null, null, true, null)
            };

            var hitEffect = new BattleSkillEffect
            {
                EffectTypeCode = BattleCodes.Damage,
                TargetTypeCode = primaryEffect.TargetTypeCode,
                DamageSchoolCode = primaryEffect.DamageSchoolCode ?? BattleCodes.Physical,
                BaseValue = primaryEffect.BaseValue,
                Scalings = primaryEffect.Scalings,
                Parameters = hitParams,
                DisplayOrder = i
            };

            var hitContext = new BattleEffectContext
            {
                Effect = hitEffect,
                Skill = context.Skill,
                Actor = context.Actor,
                Target = target,
                SelectedTargets = [target],
                Combatants = context.Combatants,
                Random = context.Random,
                Round = context.Round,
                Turn = context.Turn,
                ActionId = context.ActionId,
                TimelineOffsetMs = i <= hitOffsets.Length ? hitOffsets[i - 1] : 800 + i * 200,
                PhaseCode = "IMPACT"
            };

            var hitResult = _damageHandler.ExecuteDamage(hitContext);
            foreach (var evt in hitResult.EmittedEvents)
            {
                result.Events.Add(new PendingBattleEvent
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
                    IsCrit = evt.IsCrit,
                    HitIndex = i,
                    ExecutionGroup = $"HIT_{i}",
                    TimelineOffsetMs = hitContext.TimelineOffsetMs,
                    PhaseCode = "IMPACT"
                });
            }

            if (hitResult.WasKilled)
            {
                result.DefeatedTargetIds.Add(target.Id);
                break;
            }
        }

        return result;
    }
}
