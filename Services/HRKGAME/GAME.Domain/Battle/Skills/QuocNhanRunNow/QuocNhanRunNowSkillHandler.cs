using GAME.Domain.Battle.Effects;
using GAME.Domain.Battle.Targets;

namespace GAME.Domain.Battle.Skills.QuocNhanRunNow;

public sealed class QuocNhanRunNowSkillHandler : ISkillHandler
{
    private readonly DefaultSkillHandler _defaultHandler;
    private readonly DamageEffectHandler _damageHandler;

    public QuocNhanRunNowSkillHandler(
        DefaultSkillHandler defaultHandler,
        BattleEffectHandlerRegistry effectHandlers)
    {
        _defaultHandler = defaultHandler;
        _damageHandler = (effectHandlers.GetRequired(BattleCodes.Damage) as DamageEffectHandler) ?? new DamageEffectHandler();
    }

    public static QuocNhanRunNowSkillHandler Create(DefaultSkillHandler defaultHandler) =>
        new(defaultHandler, BattleEffectHandlerRegistry.CreateDefault());

    public bool CanHandle(BattleSkill skill) =>
        skill.Id.Equals(QuocNhanRunNowSkillCodes.Basic, StringComparison.OrdinalIgnoreCase) ||
        skill.Id.Equals(QuocNhanRunNowSkillCodes.Energy, StringComparison.OrdinalIgnoreCase);

    public SkillExecutionResult Execute(SkillExecutionContext context) =>
        context.Skill.Id.Equals(QuocNhanRunNowSkillCodes.Basic, StringComparison.OrdinalIgnoreCase)
            ? ExecuteBasic(context)
            : ExecuteEnergy(context);

    private SkillExecutionResult ExecuteBasic(SkillExecutionContext context)
    {
        var result = new SkillExecutionResult();
        var effects = context.Skill.Effects.OrderBy(x => x.DisplayOrder).ToList();
        var primaryEffect = effects.FirstOrDefault(x => x.EffectTypeCode.Equals(BattleCodes.Damage, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"Skill '{context.Skill.Id}' has no Damage effect configured.");

        // Resolve targets in the same vertical lane
        var laneTargets = _defaultHandler.ResolveTargets(primaryEffect.TargetTypeCode, context, primaryEffect)
            .Where(x => x.IsAlive)
            .ToList();

        if (laneTargets.Count == 0) return result;

        foreach (var t in laneTargets)
        {
            result.TargetedCombatantIds.Add(t.Id);
        }

        // Damage coefficients: >= 2 living targets -> 60% each; 1 living target -> 120%
        var singleTargetCoeff = primaryEffect.GetDecimal("SINGLE_TARGET_COEFF", 1.20m);
        var multiTargetCoeff = primaryEffect.GetDecimal("MULTI_TARGET_COEFF", 0.60m);
        var effectiveCoeff = laneTargets.Count >= 2 ? multiTargetCoeff : singleTargetCoeff;

        var baseEffect = new BattleSkillEffect
        {
            EffectTypeCode = BattleCodes.Damage,
            TargetTypeCode = primaryEffect.TargetTypeCode,
            DamageSchoolCode = BattleCodes.Magic,
            BaseValue = 0,
            Scalings = [new BattleEffectScaling("MAGIC_DAMAGE", effectiveCoeff, 0m)],
            Parameters = primaryEffect.Parameters
        };

        // 1. Resolve basic damage for each target in the lane
        var hitTargets = new List<BattleCombatant>();
        foreach (var target in laneTargets)
        {
            if (!target.IsAlive) continue;

            var dmgContext = new BattleEffectContext
            {
                Effect = baseEffect,
                Skill = context.Skill,
                Actor = context.Actor,
                Target = target,
                SelectedTargets = laneTargets,
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
                hitTargets.Add(target);
            }
            if (dmgResult.WasKilled)
            {
                result.DefeatedTargetIds.Add(target.Id);
            }
        }

        // 2. Check for CHAY_NGAY_DI trigger
        // If multiple targets in the lane have the mark, prioritize mark on the front-most target
        var targetWithMark = laneTargets.FirstOrDefault(t =>
            t.StatusEffects.Any(s => s.EffectTypeCode.Equals(BattleCodes.ChayNgayDi, StringComparison.OrdinalIgnoreCase) && s.RemainingTurns > 0));

        if (targetWithMark != null)
        {
            // Only consume the mark on this front-most target
            var markStatus = targetWithMark.StatusEffects.FirstOrDefault(s =>
                s.EffectTypeCode.Equals(BattleCodes.ChayNgayDi, StringComparison.OrdinalIgnoreCase) && s.RemainingTurns > 0);

            if (markStatus != null)
            {
                targetWithMark.StatusEffects.Remove(markStatus);

                result.Events.Add(new PendingBattleEvent
                {
                    EventType = "STATUS_EXPIRED",
                    ActorId = context.Actor.Id,
                    TargetId = targetWithMark.Id,
                    SkillId = markStatus.SourceSkillId,
                    EffectTypeCode = BattleCodes.ChayNgayDi,
                    StatusInstanceId = markStatus.InstanceId,
                    TimelineOffsetMs = 1000,
                    PhaseCode = "IMPACT"
                });
            }

            // Empowered flame trail sweeps through the same vertical lane: +75% Magic Damage and STUN check
            var trailDamageCoeff = primaryEffect.GetDecimal("TRAIL_DAMAGE_COEFF", 0.75m);
            var livingLaneTargets = laneTargets.Where(t => t.IsAlive).ToList();

            var trailEffect = new BattleSkillEffect
            {
                EffectTypeCode = BattleCodes.Damage,
                TargetTypeCode = BattleCodes.EnemySingle,
                DamageSchoolCode = BattleCodes.Magic,
                BaseValue = 0,
                Scalings = [new BattleEffectScaling("MAGIC_DAMAGE", trailDamageCoeff, 0m)],
                Parameters = new Dictionary<string, BattleSkillEffectParameter>
                {
                    ["CAN_CRIT"] = new("CAN_CRIT", null, null, false, null)
                }
            };

            foreach (var flameTarget in livingLaneTargets)
            {
                var trailContext = new BattleEffectContext
                {
                    Effect = trailEffect,
                    Skill = context.Skill,
                    Actor = context.Actor,
                    Target = flameTarget,
                    SelectedTargets = livingLaneTargets,
                    Combatants = context.Combatants,
                    Random = context.Random,
                    Round = context.Round,
                    Turn = context.Turn,
                    ActionId = context.ActionId,
                    TimelineOffsetMs = 1100,
                    PhaseCode = "IMPACT"
                };

                var trailResult = _damageHandler.ExecuteDamage(trailContext);
                foreach (var evt in trailResult.EmittedEvents)
                {
                    result.Events.Add(evt);
                }

                if (trailResult.WasKilled && !result.DefeatedTargetIds.Contains(flameTarget.Id))
                {
                    result.DefeatedTargetIds.Add(flameTarget.Id);
                }

                // Stun check: 1 turn
                if (flameTarget.IsAlive)
                {
                    var targetResistance = BattleStatCalculator.GetEffectiveStat(flameTarget, "RESISTANCE");
                    var roll = (decimal)context.Random.NextDouble() * 100m;
                    var stunChance = Math.Clamp(100m - targetResistance, 15m, 100m);

                    if (roll < stunChance)
                    {
                        var stunInstanceId = $"{context.Actor.Id}:{context.Skill.Id}:{BattleCodes.Stun}:{flameTarget.Id}";
                        var existingStun = flameTarget.StatusEffects.FirstOrDefault(s =>
                            s.EffectTypeCode.Equals(BattleCodes.Stun, StringComparison.OrdinalIgnoreCase));

                        if (existingStun == null)
                        {
                            flameTarget.StatusEffects.Add(new BattleStatusEffect
                            {
                                InstanceId = stunInstanceId,
                                EffectTypeCode = BattleCodes.Stun,
                                SourceSkillId = context.Skill.Id,
                                SourceHeroId = context.Actor.Id,
                                RemainingTurns = 1,
                                AppliedTurn = context.Turn,
                                Stacks = 1,
                                MaxStacks = 1,
                                Dispellable = true
                            });
                        }
                        else
                        {
                            existingStun.RemainingTurns = 1;
                        }

                        result.Events.Add(new PendingBattleEvent
                        {
                            EventType = "STATUS_APPLIED",
                            ActorId = context.Actor.Id,
                            TargetId = flameTarget.Id,
                            SkillId = context.Skill.Id,
                            EffectTypeCode = BattleCodes.Stun,
                            RemainingTurns = 1,
                            TimelineOffsetMs = 1200,
                            PhaseCode = "IMPACT"
                        });
                    }
                }
            }
        }

        return result;
    }

    private SkillExecutionResult ExecuteEnergy(SkillExecutionContext context)
    {
        var result = new SkillExecutionResult();
        var primaryEffect = context.Skill.Effects.FirstOrDefault()
            ?? throw new InvalidOperationException($"Skill '{context.Skill.Id}' has no effects configured.");

        // Targets: Front row living enemies (1, 3, 5). If 0 alive in front row, fallback to back row (2, 4).
        var targetCode = primaryEffect.TargetTypeCode ?? BattleCodes.EnemyFrontRowWithBackRowFallback;
        var targets = _defaultHandler.ResolveTargets(targetCode, context, primaryEffect);

        if (targets.Count == 0) return result;

        var mrDebuffPercent = primaryEffect.GetDecimal("MAGIC_RESISTANCE_DEBUFF_PERCENT", -30m);

        foreach (var target in targets)
        {
            result.TargetedCombatantIds.Add(target.Id);

            var instanceId = $"{context.Actor.Id}:{context.Skill.Id}:{BattleCodes.ChayNgayDi}:{target.Id}";
            var existing = target.StatusEffects.FirstOrDefault(s =>
                s.EffectTypeCode.Equals(BattleCodes.ChayNgayDi, StringComparison.OrdinalIgnoreCase));

            if (existing != null)
            {
                existing.RemainingTurns = 3;
                result.Events.Add(new PendingBattleEvent
                {
                    EventType = BattleCodes.StatusRefreshed,
                    ActorId = context.Actor.Id,
                    TargetId = target.Id,
                    SkillId = context.Skill.Id,
                    EffectTypeCode = BattleCodes.ChayNgayDi,
                    RemainingTurns = 3,
                    StatusInstanceId = existing.InstanceId,
                    TimelineOffsetMs = 700,
                    PhaseCode = "CAST"
                });
            }
            else
            {
                target.StatusEffects.Add(new BattleStatusEffect
                {
                    InstanceId = instanceId,
                    EffectTypeCode = BattleCodes.ChayNgayDi,
                    SourceSkillId = context.Skill.Id,
                    SourceHeroId = context.Actor.Id,
                    RemainingTurns = 3,
                    AppliedTurn = context.Turn,
                    Stacks = 1,
                    MaxStacks = 1,
                    Value = mrDebuffPercent,
                    Dispellable = true,
                    ScaleModifiersWithStacks = false,
                    StatModifiers = [new BattleStatModifier("MAGIC_RESISTANCE", "PERCENT", mrDebuffPercent, "Kháng Phép")]
                });

                result.Events.Add(new PendingBattleEvent
                {
                    EventType = "STATUS_APPLIED",
                    ActorId = context.Actor.Id,
                    TargetId = target.Id,
                    SkillId = context.Skill.Id,
                    EffectTypeCode = BattleCodes.ChayNgayDi,
                    RemainingTurns = 3,
                    Value = (int)mrDebuffPercent,
                    StatusInstanceId = instanceId,
                    StatModifiers = [new BattleStatModifier("MAGIC_RESISTANCE", "PERCENT", mrDebuffPercent, "Kháng Phép")],
                    TimelineOffsetMs = 700,
                    PhaseCode = "CAST"
                });
            }
        }

        return result;
    }
}
