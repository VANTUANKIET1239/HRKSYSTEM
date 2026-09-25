using GAME.Domain.Battle.Effects;
using GAME.Domain.Battle.Targets;

namespace GAME.Domain.Battle.Skills.ChuanMen;

public sealed class ChuanMenSkillHandler : ISkillHandler
{
    private readonly DefaultSkillHandler _defaultHandler;
    private readonly BattleEffectHandlerRegistry _effectHandlers;
    private readonly DamageEffectHandler _damageHandler;

    public ChuanMenSkillHandler(
        DefaultSkillHandler defaultHandler,
        BattleEffectHandlerRegistry effectHandlers,
        BattleTargetSelectorRegistry? targetSelectors = null)
    {
        _defaultHandler = defaultHandler;
        _effectHandlers = effectHandlers;
        _damageHandler = (effectHandlers.GetRequired(BattleCodes.Damage) as DamageEffectHandler) ?? new DamageEffectHandler();
    }

    public static ChuanMenSkillHandler Create(DefaultSkillHandler defaultHandler) =>
        new(defaultHandler, BattleEffectHandlerRegistry.CreateDefault());

    public static ChuanMenSkillHandler Create(
        DefaultSkillHandler defaultHandler,
        BattleEffectHandlerRegistry effectHandlers,
        BattleTargetSelectorRegistry? targetSelectors = null) =>
        new(defaultHandler, effectHandlers, targetSelectors);

    public bool CanHandle(BattleSkill skill) =>
        skill.Id.Equals(ChuanMenSkillCodes.Basic, StringComparison.OrdinalIgnoreCase) ||
        skill.Id.Equals(ChuanMenSkillCodes.Ultimate, StringComparison.OrdinalIgnoreCase);

    public SkillExecutionResult Execute(SkillExecutionContext context) =>
        context.Skill.Id.Equals(ChuanMenSkillCodes.Basic, StringComparison.OrdinalIgnoreCase)
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
            Turn = context.Turn,
            ActionId = context.ActionId
        };

        var damageResult = _damageHandler.ExecuteDamage(effectContext);
        foreach (var evt in damageResult.EmittedEvents)
        {
            result.Events.Add(Stamp(evt, "PUNCH", 1, 400, "IMPACT"));
        }

        if (damageResult.WasHit)
        {
            result.BasicAttackHitTargetIds.Add(target.Id);
        }
        if (damageResult.WasKilled && !result.DefeatedTargetIds.Contains(target.Id))
        {
            result.DefeatedTargetIds.Add(target.Id);
        }

        return result;
    }

    private SkillExecutionResult ExecuteUltimate(SkillExecutionContext context)
    {
        var ricardoStatus = context.Actor.StatusEffects.FirstOrDefault(x =>
            x.EffectTypeCode.Equals(ChuanMenSkillCodes.RicardoStatus, StringComparison.OrdinalIgnoreCase) &&
            (x.RemainingTurns > 0 || x.RemainingTurns == -1));

        var empoweredDamageEffect = context.Skill.Effects.FirstOrDefault(x =>
            !string.IsNullOrEmpty(x.ExecutionGroup) &&
            x.ExecutionGroup.Equals("EMPOWERED", StringComparison.OrdinalIgnoreCase) &&
            x.EffectTypeCode.Equals(BattleCodes.Damage, StringComparison.OrdinalIgnoreCase));

        var requiredStacks = empoweredDamageEffect?.GetInt("REQUIRED_RICARDO_STACKS", 6) ?? 6;
        var isEmpowered = ricardoStatus != null && ricardoStatus.Stacks >= requiredStacks;

        return isEmpowered
            ? ExecuteEmpoweredUltimate(context, ricardoStatus!, empoweredDamageEffect!)
            : ExecuteNormalUltimate(context);
    }

    private SkillExecutionResult ExecuteNormalUltimate(SkillExecutionContext context)
    {
        var result = new SkillExecutionResult();

        var normalEffects = context.Skill.Effects
            .Where(x => string.IsNullOrEmpty(x.ExecutionGroup) ||
                        x.ExecutionGroup.Equals("NORMAL", StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => x.DisplayOrder)
            .ToList();

        var damageEffect = normalEffects.FirstOrDefault(x =>
            x.EffectTypeCode.Equals(BattleCodes.Damage, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"Skill '{context.Skill.Id}' is missing a normal '{BattleCodes.Damage}' effect.");

        var target = _defaultHandler.ResolveTargets(damageEffect.TargetTypeCode, context).FirstOrDefault();
        if (target == null) return result;

        // 1. Damage to primary target (Enemy same lane back row prioritized)
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
            Turn = context.Turn,
            ActionId = context.ActionId
        };

        var damageResult = _damageHandler.ExecuteDamage(effectContext);
        foreach (var evt in damageResult.EmittedEvents)
        {
            result.Events.Add(Stamp(evt, "NORMAL", 1, 1400, "IMPACT"));
        }

        if (damageResult.WasKilled && !result.DefeatedTargetIds.Contains(target.Id))
        {
            result.DefeatedTargetIds.Add(target.Id);
        }

        // 2. Stun target if survived
        if (target.IsAlive)
        {
            var stunEffect = normalEffects.FirstOrDefault(x =>
                x.EffectTypeCode.Equals(BattleCodes.Stun, StringComparison.OrdinalIgnoreCase));

            if (stunEffect != null)
            {
                var roll = (decimal)context.Random.NextDouble() * 100m;
                if (roll <= stunEffect.ChancePercent)
                {
                    var handler = _effectHandlers.GetRequired(stunEffect.EffectTypeCode);
                    var stunEvents = handler.Apply(new BattleEffectContext
                    {
                        Effect = stunEffect,
                        Skill = context.Skill,
                        Actor = context.Actor,
                        Target = target,
                        SelectedTargets = [target],
                        Combatants = context.Combatants,
                        Random = context.Random,
                        Round = context.Round,
                        Turn = context.Turn,
                        ActionId = context.ActionId
                    });
                    foreach (var evt in stunEvents)
                    {
                        result.Events.Add(Stamp(evt, "NORMAL", 1, 1500, "STATUS"));
                    }
                }
            }
        }

        // 3. Apply or maintain RICARDO buff on Chuẩn Men (Actor)
        var ricardoEffect = normalEffects.FirstOrDefault(x =>
            x.EffectTypeCode.Equals(ChuanMenSkillCodes.RicardoStatus, StringComparison.OrdinalIgnoreCase));

        if (ricardoEffect != null)
        {
            var handler = _effectHandlers.GetRequired(ricardoEffect.EffectTypeCode);
            var ricardoEvents = handler.Apply(new BattleEffectContext
            {
                Effect = ricardoEffect,
                Skill = context.Skill,
                Actor = context.Actor,
                Target = context.Actor,
                SelectedTargets = [context.Actor],
                Combatants = context.Combatants,
                Random = context.Random,
                Round = context.Round,
                Turn = context.Turn,
                ActionId = context.ActionId
            });
            foreach (var evt in ricardoEvents)
            {
                result.Events.Add(Stamp(evt, "NORMAL", null, 1600, "STATUS"));
            }
        }

        return result;
    }

    private SkillExecutionResult ExecuteEmpoweredUltimate(
        SkillExecutionContext context,
        BattleStatusEffect ricardoStatus,
        BattleSkillEffect empoweredDamageEffect)
    {
        var result = new SkillExecutionResult();

        // 1. Identify empowered cast & snapshot alive enemies
        var aliveEnemies = context.Combatants
            .Where(x => x.Team != context.Actor.Team && x.IsAlive)
            .OrderBy(x => x.Position)
            .ThenBy(x => x.Id)
            .ToList();

        if (aliveEnemies.Count == 0) return result;

        // 2. Emit empowered cast start event
        result.Events.Add(new PendingBattleEvent
        {
            EventType = BattleCodes.RicardoRageReady,
            ActorId = context.Actor.Id,
            TargetId = context.Actor.Id,
            SkillId = context.Skill.Id,
            EffectTypeCode = ChuanMenSkillCodes.RicardoStatus,
            PreviousStacks = ricardoStatus.Stacks,
            CurrentStacks = ricardoStatus.Stacks,
            MaxStacks = ricardoStatus.MaxStacks,
            ExecutionGroup = "EMPOWERED",
            TimelineOffsetMs = 0,
            PhaseCode = "PREPARE"
        });

        // 3. AoE Damage to all alive enemies (individual crit and damage calculation)
        foreach (var enemy in aliveEnemies)
        {
            var hitContext = new BattleEffectContext
            {
                Effect = empoweredDamageEffect,
                Skill = context.Skill,
                Actor = context.Actor,
                Target = enemy,
                SelectedTargets = aliveEnemies,
                Combatants = context.Combatants,
                Random = context.Random,
                Round = context.Round,
                Turn = context.Turn,
                ActionId = context.ActionId
            };

            var hitResult = _damageHandler.ExecuteDamage(hitContext);
            foreach (var evt in hitResult.EmittedEvents)
            {
                result.Events.Add(Stamp(evt, "EMPOWERED", null, 1500, "AOE_DAMAGE"));
            }

            if (hitResult.WasKilled && !result.DefeatedTargetIds.Contains(enemy.Id))
            {
                result.DefeatedTargetIds.Add(enemy.Id);
            }
        }

        // 4. Stun exactly one random surviving enemy
        var survivingEnemies = aliveEnemies.Where(x => x.IsAlive).ToList();
        if (survivingEnemies.Count > 0)
        {
            var empoweredStunEffect = context.Skill.Effects.FirstOrDefault(x =>
                !string.IsNullOrEmpty(x.ExecutionGroup) &&
                x.ExecutionGroup.Equals("EMPOWERED", StringComparison.OrdinalIgnoreCase) &&
                x.EffectTypeCode.Equals(BattleCodes.Stun, StringComparison.OrdinalIgnoreCase));

            if (empoweredStunEffect != null)
            {
                var roll = (decimal)context.Random.NextDouble() * 100m;
                if (roll <= empoweredStunEffect.ChancePercent)
                {
                    var stunIndex = context.Random.Next(0, survivingEnemies.Count);
                    var stunTarget = survivingEnemies[stunIndex];

                    var handler = _effectHandlers.GetRequired(empoweredStunEffect.EffectTypeCode);
                    var stunEvents = handler.Apply(new BattleEffectContext
                    {
                        Effect = empoweredStunEffect,
                        Skill = context.Skill,
                        Actor = context.Actor,
                        Target = stunTarget,
                        SelectedTargets = [stunTarget],
                        Combatants = context.Combatants,
                        Random = context.Random,
                        Round = context.Round,
                        Turn = context.Turn,
                        ActionId = context.ActionId
                    });

                    foreach (var evt in stunEvents)
                    {
                        result.Events.Add(Stamp(evt, "EMPOWERED", null, 1650, "RANDOM_STUN"));
                    }
                }
            }
        }

        // 5. Consume RICARDO buff and reset stacks after execution
        var shouldConsume = empoweredDamageEffect.GetBool("CONSUME_RICARDO_AFTER_EXECUTION", true);
        if (shouldConsume)
        {
            var prevStacks = ricardoStatus.Stacks;
            context.Actor.StatusEffects.Remove(ricardoStatus);
            ricardoStatus.Stacks = 0;

            result.Events.Add(new PendingBattleEvent
            {
                EventType = BattleCodes.StatusRemoved,
                ActorId = context.Actor.Id,
                TargetId = context.Actor.Id,
                SkillId = context.Skill.Id,
                EffectTypeCode = ChuanMenSkillCodes.RicardoStatus,
                PreviousStacks = prevStacks,
                CurrentStacks = 0,
                MaxStacks = ricardoStatus.MaxStacks,
                ExecutionGroup = "EMPOWERED",
                TimelineOffsetMs = 2100,
                PhaseCode = "RETURN"
            });

            result.Events.Add(new PendingBattleEvent
            {
                EventType = BattleCodes.RicardoConsumed,
                ActorId = context.Actor.Id,
                TargetId = context.Actor.Id,
                SkillId = context.Skill.Id,
                EffectTypeCode = ChuanMenSkillCodes.RicardoStatus,
                PreviousStacks = prevStacks,
                CurrentStacks = 0,
                MaxStacks = ricardoStatus.MaxStacks,
                ExecutionGroup = "EMPOWERED",
                TimelineOffsetMs = 2100,
                PhaseCode = "RETURN"
            });
        }

        // 6. Complete event
        result.Events.Add(new PendingBattleEvent
        {
            EventType = "SKILL_COMPLETED",
            ActorId = context.Actor.Id,
            SkillId = context.Skill.Id,
            ExecutionGroup = "EMPOWERED",
            TimelineOffsetMs = 2400,
            PhaseCode = "COMPLETE"
        });

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
            PreviousStacks = evt.PreviousStacks,
            CurrentStacks = evt.CurrentStacks,
            MaxStacks = evt.MaxStacks,
            StatModifiers = evt.StatModifiers,
            ExecutionGroup = executionGroup ?? evt.ExecutionGroup,
            HitIndex = hitIndex ?? evt.HitIndex,
            TimelineOffsetMs = timelineOffsetMs,
            PhaseCode = phaseCode ?? evt.PhaseCode
        };
}
