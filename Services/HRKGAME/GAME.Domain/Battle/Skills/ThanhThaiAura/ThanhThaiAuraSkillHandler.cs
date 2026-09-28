using GAME.Domain.Battle.Effects;
using GAME.Domain.Battle.Targets;

namespace GAME.Domain.Battle.Skills.ThanhThaiAura;

public sealed class ThanhThaiAuraSkillHandler : ISkillHandler
{
    private readonly DefaultSkillHandler _defaultHandler;
    private readonly BattleEffectHandlerRegistry _effectHandlers;
    private readonly DamageEffectHandler _damageHandler;

    public ThanhThaiAuraSkillHandler(
        DefaultSkillHandler defaultHandler,
        BattleEffectHandlerRegistry effectHandlers,
        BattleTargetSelectorRegistry? targetSelectors = null)
    {
        _defaultHandler = defaultHandler;
        _effectHandlers = effectHandlers;
        _damageHandler = (effectHandlers.GetRequired(BattleCodes.Damage) as DamageEffectHandler) ?? new DamageEffectHandler();
    }

    public static ThanhThaiAuraSkillHandler Create(DefaultSkillHandler defaultHandler) =>
        new(defaultHandler, BattleEffectHandlerRegistry.CreateDefault());

    public static ThanhThaiAuraSkillHandler Create(
        DefaultSkillHandler defaultHandler,
        BattleEffectHandlerRegistry effectHandlers,
        BattleTargetSelectorRegistry? targetSelectors = null) =>
        new(defaultHandler, effectHandlers, targetSelectors);

    public bool CanHandle(BattleSkill skill) =>
        skill.Id.Equals(ThanhThaiAuraSkillCodes.Basic, StringComparison.OrdinalIgnoreCase) ||
        skill.Id.Equals(ThanhThaiAuraSkillCodes.Ultimate, StringComparison.OrdinalIgnoreCase);

    public SkillExecutionResult Execute(SkillExecutionContext context) =>
        context.Skill.Id.Equals(ThanhThaiAuraSkillCodes.Basic, StringComparison.OrdinalIgnoreCase)
            ? ExecuteBasic(context)
            : ExecuteUltimate(context);

    public SkillExecutionResult ExecuteBasic(SkillExecutionContext context)
    {
        var result = new SkillExecutionResult();

        var effects = context.Skill.Effects.OrderBy(x => x.DisplayOrder).ToList();
        var primaryEffect = effects.FirstOrDefault() ??
            throw new InvalidOperationException($"Skill '{context.Skill.Id}' has no effects configured.");

        // Resolve single target
        var target = _defaultHandler.ResolveTargets(primaryEffect.TargetTypeCode, context).FirstOrDefault();
        if (target == null) return result;

        result.TargetedCombatantIds.Add(target.Id);

        var hasFullAura = context.Actor.StatusEffects.Any(x =>
            (x.RemainingTurns > 0 || x.RemainingTurns == -1) &&
            x.EffectTypeCode.Equals(ThanhThaiAuraSkillCodes.StatusFullAura, StringComparison.OrdinalIgnoreCase));

        var auraGainBasic = primaryEffect.GetInt("AURA_GAIN_BASIC", 10);
        var locMaxStacks = primaryEffect.GetInt("LOSS_OF_CONFIDENCE_MAX_STACKS", 3);
        var locDurationTurns = primaryEffect.GetInt("LOSS_OF_CONFIDENCE_DURATION_TURNS", 6);
        var locDamageTakenPerStack = primaryEffect.GetDecimal("LOSS_OF_CONFIDENCE_DAMAGE_TAKEN_PER_STACK", 10m);

        var anyHitConnected = false;

        // Execute damage effects (Physical 70%, Magic 60%)
        foreach (var effect in effects.Where(e => e.EffectTypeCode.Equals(BattleCodes.Damage, StringComparison.OrdinalIgnoreCase)))
        {
            if (!target.IsAlive) break;

            var damageEffect = WithAuraDamageBonus(
                effect,
                context.Actor.GetResource(ThanhThaiAuraSkillCodes.ResourceAura, 0));

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

            var dmgResult = _damageHandler.ExecuteDamage(effectContext);
            foreach (var evt in dmgResult.EmittedEvents)
            {
                result.Events.Add(Stamp(evt, "SWEEP", 1, 800, "IMPACT"));
            }

            if (dmgResult.WasHit)
            {
                anyHitConnected = true;
                result.BasicAttackHitTargetIds.Add(target.Id);
            }
            if (dmgResult.WasKilled && !result.DefeatedTargetIds.Contains(target.Id))
            {
                result.DefeatedTargetIds.Add(target.Id);
            }
        }

        // If Full Aura and hit successfully, apply 1 stack of LOSS_OF_CONFIDENCE
        if (hasFullAura && anyHitConnected && target.IsAlive)
        {
            var locEvents = ApplyLossOfConfidence(context.Actor, target, context.Skill.Id, locDurationTurns, locMaxStacks, locDamageTakenPerStack, 1200, "STATUS");
            result.Events.AddRange(locEvents);
        }

        // Basic attack always awards +10 Bá Khí (even if evaded/missed)
        var auraEvents = ThanhThaiAuraResourceHandler.GainAura(
            context.Actor,
            auraGainBasic,
            ThanhThaiAuraSkillCodes.ReasonBasicAttack,
            context.ActionId,
            context.Skill.Id,
            target.Id,
            timelineOffsetMs: context.Skill.Animation.TotalDurationMs,
            phaseCode: "RECOVERY");
        result.Events.AddRange(auraEvents);

        return result;
    }

    public SkillExecutionResult ExecuteUltimate(SkillExecutionContext context)
    {
        var result = new SkillExecutionResult();

        // 1. Snapshot Bá Khí before any consumption or damage
        var snapshotAura = context.Actor.GetResource(ThanhThaiAuraSkillCodes.ResourceAura, 0);

        var effects = context.Skill.Effects.OrderBy(x => x.DisplayOrder).ToList();
        var primaryEffect = effects.FirstOrDefault() ??
            throw new InvalidOperationException($"Skill '{context.Skill.Id}' has no effects configured.");

        // Read data-driven parameters
        var fullAuraThreshold = primaryEffect.GetInt("FULL_AURA_THRESHOLD", 100);
        var auraConsumption = primaryEffect.GetInt("FULL_AURA_CONSUMPTION", 100);
        var defaultTargets = primaryEffect.GetInt("DEFAULT_MAX_TARGETS", 3);
        var fullTargets = primaryEffect.GetInt("FULL_AURA_MAX_TARGETS", 4);
        var chainRange = (double)primaryEffect.GetDecimal("CHAIN_RANGE", 1.50m);
        var decayPercent = primaryEffect.GetDecimal("CHAIN_DAMAGE_DECAY_PERCENT", 15.00m);
        var auraGainDefeat = primaryEffect.GetInt("AURA_GAIN_DEFEAT", 15);
        var locMaxStacks = primaryEffect.GetInt("LOSS_OF_CONFIDENCE_MAX_STACKS", 3);
        var locDurationTurns = primaryEffect.GetInt("LOSS_OF_CONFIDENCE_DURATION_TURNS", 6);
        var locDamageTakenPerStack = primaryEffect.GetDecimal("LOSS_OF_CONFIDENCE_DAMAGE_TAKEN_PER_STACK", 10m);
        var locApplyChance = primaryEffect.GetDecimal("LOSS_OF_CONFIDENCE_SKILL_APPLY_CHANCE", 40m);

        // Detonation coefficients
        var detPhysical1 = primaryEffect.GetDecimal("DETONATION_PHYSICAL_STACK_1", 0.30m);
        var detMagic1 = primaryEffect.GetDecimal("DETONATION_MAGIC_STACK_1", 0.20m);
        var detPhysical2 = primaryEffect.GetDecimal("DETONATION_PHYSICAL_STACK_2", 0.60m);
        var detMagic2 = primaryEffect.GetDecimal("DETONATION_MAGIC_STACK_2", 0.40m);
        var detPhysical3 = primaryEffect.GetDecimal("DETONATION_PHYSICAL_STACK_3", 1.00m);
        var detMagic3 = primaryEffect.GetDecimal("DETONATION_MAGIC_STACK_3", 0.70m);

        // 2. Determine max targets and detonation capability
        var maxTargets = snapshotAura >= fullAuraThreshold ? fullTargets : defaultTargets;
        var consumesAura = snapshotAura >= fullAuraThreshold;

        if (consumesAura)
        {
            result.Events.Add(new PendingBattleEvent
            {
                EventType = BattleCodes.ThanhThaiEmpoweredCast,
                ActorId = context.Actor.Id,
                SkillId = context.Skill.Id,
                ExecutionGroup = "EMPOWERED",
                ActionId = context.ActionId,
                TimelineOffsetMs = 0,
                PhaseCode = "CAST"
            });
        }

        var livingEnemies = context.Combatants.Where(x => x.Team != context.Actor.Team && x.IsAlive).ToList();
        if (livingEnemies.Count == 0) return result;

        // Select chain targets deterministically
        var chainTargets = ThanhThaiFormationGrid.SelectChainTargets(livingEnemies, maxTargets, chainRange);
        var chainMultipliers = BuildChainAllocationMultipliers(chainTargets, maxTargets, decayPercent);

        var physicalTemplate = effects.FirstOrDefault(e => e.DamageSchoolCode == BattleCodes.Physical) ?? primaryEffect;
        var magicTemplate = effects.FirstOrDefault(e => e.DamageSchoolCode == BattleCodes.Magic);

        var killedInThisCast = new List<long>();

        // 3. Execute chain damage & detonation on each target in order
        for (var i = 0; i < chainTargets.Count; i++)
        {
            var target = chainTargets[i];
            if (!target.IsAlive) continue;

            result.TargetedCombatantIds.Add(target.Id);

            var decayMult = chainMultipliers.GetValueOrDefault(target.Id, 1m);
            var hitTimingOffset = 600 + (i * 350);

            // Roll once per actual target, not once per physical/magical component.
            if (context.Random.NextDouble() * 100d < (double)locApplyChance)
            {
                var locEvents = ApplyLossOfConfidence(
                    context.Actor, target, context.Skill.Id, locDurationTurns, locMaxStacks,
                    locDamageTakenPerStack, hitTimingOffset + 100, "STATUS");
                result.Events.AddRange(locEvents);
            }

            // Snapshot target's LOSS_OF_CONFIDENCE before detonation
            var locStatus = target.StatusEffects.FirstOrDefault(x =>
                x.EffectTypeCode.Equals(ThanhThaiAuraSkillCodes.StatusLossOfConfidence, StringComparison.OrdinalIgnoreCase) &&
                (x.RemainingTurns > 0 || x.RemainingTurns == -1));
            var initialLocStacks = locStatus?.Stacks ?? 0;

            var targetHitSuccessful = false;

            // Physical Hit
            var physEffect = CloneEffectWithDecay(physicalTemplate, decayMult, snapshotAura);
            var physCtx = new BattleEffectContext
            {
                Effect = physEffect,
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
            var physResult = _damageHandler.ExecuteDamage(physCtx);
            foreach (var evt in physResult.EmittedEvents)
            {
                result.Events.Add(Stamp(evt, $"CHAIN_{i + 1}", i + 1, hitTimingOffset, "IMPACT"));
            }
            if (physResult.WasHit) targetHitSuccessful = true;
            if (physResult.WasKilled && !killedInThisCast.Contains(target.Id))
            {
                killedInThisCast.Add(target.Id);
                result.DefeatedTargetIds.Add(target.Id);
            }

            // Magic Hit
            if (magicTemplate != null && target.IsAlive)
            {
                var magEffect = CloneEffectWithDecay(magicTemplate, decayMult, snapshotAura);
                var magCtx = new BattleEffectContext
                {
                    Effect = magEffect,
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
                var magResult = _damageHandler.ExecuteDamage(magCtx);
                foreach (var evt in magResult.EmittedEvents)
                {
                    result.Events.Add(Stamp(evt, $"CHAIN_{i + 1}", i + 1, hitTimingOffset + 50, "IMPACT"));
                }
                if (magResult.WasHit) targetHitSuccessful = true;
                if (magResult.WasKilled && !killedInThisCast.Contains(target.Id))
                {
                    killedInThisCast.Add(target.Id);
                    result.DefeatedTargetIds.Add(target.Id);
                }
            }

            // 4. Detonation: only if Aura >= 75, target had LOSS_OF_CONFIDENCE, and was hit successfully
            if (initialLocStacks >= locMaxStacks && targetHitSuccessful && target.IsAlive)
            {
                var (physCoeff, magCoeff) = initialLocStacks switch
                {
                    >= 3 => (detPhysical3, detMagic3),
                    2 => (detPhysical2, detMagic2),
                    _ => (detPhysical1, detMagic1)
                };

                var detOffset = hitTimingOffset + 150;
                var detTotalDamage = 0;

                // Physical detonation instance (cannot crit, can kill)
                var detPhysEffect = CreateDetonationEffect(BattleCodes.Physical, physCoeff, snapshotAura);
                var detPhysCtx = new BattleEffectContext
                {
                    Effect = detPhysEffect,
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
                var detPhysResult = _damageHandler.ExecuteDamage(detPhysCtx);
                detTotalDamage += detPhysResult.ActualDamage;
                foreach (var evt in detPhysResult.EmittedEvents)
                {
                    result.Events.Add(Stamp(evt, "DETONATE", i + 1, detOffset, "IMPACT"));
                }
                if (detPhysResult.WasKilled && !killedInThisCast.Contains(target.Id))
                {
                    killedInThisCast.Add(target.Id);
                    result.DefeatedTargetIds.Add(target.Id);
                }

                // Magic detonation instance (if target still alive)
                if (target.IsAlive)
                {
                    var detMagEffect = CreateDetonationEffect(BattleCodes.Magic, magCoeff, snapshotAura);
                    var detMagCtx = new BattleEffectContext
                    {
                        Effect = detMagEffect,
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
                    var detMagResult = _damageHandler.ExecuteDamage(detMagCtx);
                    detTotalDamage += detMagResult.ActualDamage;
                    foreach (var evt in detMagResult.EmittedEvents)
                    {
                        result.Events.Add(Stamp(evt, "DETONATE", i + 1, detOffset + 50, "IMPACT"));
                    }
                    if (detMagResult.WasKilled && !killedInThisCast.Contains(target.Id))
                    {
                        killedInThisCast.Add(target.Id);
                        result.DefeatedTargetIds.Add(target.Id);
                    }
                }

                // Emit LOSS_OF_CONFIDENCE_DETONATED
                result.Events.Add(new PendingBattleEvent
                {
                    EventType = BattleCodes.LossOfConfidenceDetonated,
                    ActorId = context.Actor.Id,
                    TargetId = target.Id,
                    SkillId = context.Skill.Id,
                    EffectTypeCode = ThanhThaiAuraSkillCodes.StatusLossOfConfidence,
                    Value = detTotalDamage,
                    PreviousStacks = initialLocStacks,
                    CurrentStacks = 0,
                    MaxStacks = locMaxStacks,
                    TimelineOffsetMs = detOffset,
                    PhaseCode = "IMPACT"
                });

                // Consume all stacks and remove status
                target.StatusEffects.RemoveAll(x =>
                    x.EffectTypeCode.Equals(ThanhThaiAuraSkillCodes.StatusLossOfConfidence, StringComparison.OrdinalIgnoreCase));
                result.Events.Add(new PendingBattleEvent
                {
                    EventType = BattleCodes.StatusRemoved,
                    ActorId = context.Actor.Id,
                    TargetId = target.Id,
                    SkillId = context.Skill.Id,
                    EffectTypeCode = ThanhThaiAuraSkillCodes.StatusLossOfConfidence,
                    TimelineOffsetMs = detOffset + 100,
                    PhaseCode = "IMPACT"
                });
            }
        }

        // 6. Consume 75 Bá Khí (if used at >= 75 Aura)
        var recoveryTiming = 1800;
        if (consumesAura)
        {
            var consumeEvents = ThanhThaiAuraResourceHandler.ConsumeAura(
                context.Actor,
                auraConsumption,
                ThanhThaiAuraSkillCodes.ReasonSkillConsumption,
                context.ActionId,
                context.Skill.Id,
                timelineOffsetMs: recoveryTiming,
                phaseCode: "AURA_CONSUME");
            result.Events.AddRange(consumeEvents);
        }

        // 7. Defeat recovery (+15 Bá Khí per killed target)
        foreach (var killedId in killedInThisCast)
        {
            // Mark so combatant reaction won't double-trigger
            context.Actor.ProcessedDefeatedCombatantIds.Add(killedId);

            var defeatEvents = ThanhThaiAuraResourceHandler.GainAura(
                context.Actor,
                auraGainDefeat,
                ThanhThaiAuraSkillCodes.ReasonCombatantDefeated,
                context.ActionId,
                context.Skill.Id,
                killedId,
                timelineOffsetMs: recoveryTiming + 100,
                phaseCode: "RECOVERY");
            result.Events.AddRange(defeatEvents);
        }

        return result;
    }

    private static Dictionary<long, decimal> BuildChainAllocationMultipliers(
        IReadOnlyList<BattleCombatant> actualTargets,
        int allowedHitCount,
        decimal decayPercent)
    {
        var result = actualTargets.ToDictionary(x => x.Id, _ => 0m);
        if (actualTargets.Count == 0 || allowedHitCount <= 0) return result;

        for (var hitIndex = 0; hitIndex < allowedHitCount; hitIndex++)
        {
            var hitMultiplier = Math.Max(0m, 1m - (hitIndex * decayPercent / 100m));
            if (hitIndex < actualTargets.Count)
            {
                result[actualTargets[hitIndex].Id] += hitMultiplier;
                continue;
            }

            var share = hitMultiplier / actualTargets.Count;
            foreach (var target in actualTargets)
            {
                result[target.Id] += share;
            }
        }

        return result;
    }

    private static IReadOnlyList<PendingBattleEvent> ApplyLossOfConfidence(
        BattleCombatant actor,
        BattleCombatant target,
        string skillId,
        int durationTurns,
        int maxStacks,
        decimal damageBonusPerStack,
        int timelineOffsetMs,
        string phaseCode)
    {
        var events = new List<PendingBattleEvent>();
        var existing = target.StatusEffects.FirstOrDefault(x =>
            x.EffectTypeCode.Equals(ThanhThaiAuraSkillCodes.StatusLossOfConfidence, StringComparison.OrdinalIgnoreCase) &&
            (x.RemainingTurns > 0 || x.RemainingTurns == -1));

        if (existing != null)
        {
            var prevStacks = existing.Stacks;
            existing.Stacks = Math.Min(maxStacks, existing.Stacks + 1);
            existing.RemainingTurns = durationTurns; // Refresh on reapply

            events.Add(new PendingBattleEvent
            {
                EventType = BattleCodes.StatusRefreshed,
                ActorId = actor.Id,
                TargetId = target.Id,
                SkillId = skillId,
                EffectTypeCode = ThanhThaiAuraSkillCodes.StatusLossOfConfidence,
                RemainingTurns = durationTurns,
                CurrentStacks = existing.Stacks,
                MaxStacks = maxStacks,
                TimelineOffsetMs = timelineOffsetMs,
                PhaseCode = phaseCode
            });

            if (existing.Stacks != prevStacks)
            {
                events.Add(new PendingBattleEvent
                {
                    EventType = BattleCodes.StatusStackChanged,
                    ActorId = actor.Id,
                    TargetId = target.Id,
                    SkillId = skillId,
                    EffectTypeCode = ThanhThaiAuraSkillCodes.StatusLossOfConfidence,
                    Value = existing.Stacks,
                    PreviousStacks = prevStacks,
                    CurrentStacks = existing.Stacks,
                    MaxStacks = maxStacks,
                    TimelineOffsetMs = timelineOffsetMs,
                    PhaseCode = phaseCode
                });
            }
        }
        else
        {
            var newStatus = new BattleStatusEffect
            {
                InstanceId = Guid.NewGuid().ToString("N"),
                EffectTypeCode = ThanhThaiAuraSkillCodes.StatusLossOfConfidence,
                SourceSkillId = skillId,
                SourceHeroId = actor.Id,
                RemainingTurns = durationTurns,
                Stacks = 1,
                MaxStacks = maxStacks,
                DamageBonusPerStackPercent = damageBonusPerStack,
                IncomingDamageBonusPerStackPercent = damageBonusPerStack,
                IncomingDamageBonusRestrictedToSource = true,
                StatModifiers = []
            };
            target.StatusEffects.Add(newStatus);

            events.Add(new PendingBattleEvent
            {
                EventType = "STATUS_APPLIED",
                ActorId = actor.Id,
                TargetId = target.Id,
                SkillId = skillId,
                EffectTypeCode = ThanhThaiAuraSkillCodes.StatusLossOfConfidence,
                RemainingTurns = durationTurns,
                CurrentStacks = 1,
                MaxStacks = maxStacks,
                TimelineOffsetMs = timelineOffsetMs,
                PhaseCode = phaseCode
            });
        }

        return events;
    }

    private static BattleSkillEffect CloneEffectWithDecay(BattleSkillEffect source, decimal decayMultiplier, int snapshotAura)
    {
        var scalings = source.Scalings.Select(s => new BattleEffectScaling(
            s.AttributeCode,
            s.Coefficient * decayMultiplier,
            s.FlatValue * decayMultiplier)).ToList();

        var parameters = BuildParametersWithAuraDamageBonus(source, snapshotAura);

        return new BattleSkillEffect
        {
            EffectTypeCode = source.EffectTypeCode,
            TargetTypeCode = source.TargetTypeCode,
            DamageSchoolCode = source.DamageSchoolCode,
            BaseValue = source.BaseValue * decayMultiplier,
            DurationTurns = source.DurationTurns,
            ChancePercent = source.ChancePercent,
            MaxStacks = source.MaxStacks,
            DisplayOrder = source.DisplayOrder,
            ExecutionGroup = source.ExecutionGroup,
            ConditionCode = source.ConditionCode,
            Scalings = scalings,
            StatModifiers = source.StatModifiers,
            Parameters = parameters
        };
    }

    private static BattleSkillEffect CreateDetonationEffect(string school, decimal coefficient, int snapshotAura)
    {
        var attributeCode = school == BattleCodes.Magic ? "MAGIC_DAMAGE" : "ATK";
        var scalings = new List<BattleEffectScaling>
        {
            new(attributeCode, coefficient, 0m)
        };
        var parameters = new Dictionary<string, BattleSkillEffectParameter>(StringComparer.OrdinalIgnoreCase)
        {
            ["CAN_CRIT"] = new("CAN_CRIT", null, null, false, null),
            ["CAN_KILL"] = new("CAN_KILL", null, null, true, null),
            ["DAMAGE_BONUS_PERCENT"] = new(
                "DAMAGE_BONUS_PERCENT",
                snapshotAura * ThanhThaiAuraResourceHandler.DefaultDamageBonusPercentPerAura,
                null, null, null)
        };

        return new BattleSkillEffect
        {
            EffectTypeCode = BattleCodes.Damage,
            TargetTypeCode = BattleCodes.EnemySingle,
            DamageSchoolCode = school,
            BaseValue = 0m,
            ChancePercent = 100m,
            MaxStacks = 1,
            DisplayOrder = 1,
            Scalings = scalings,
            StatModifiers = [],
            Parameters = parameters
        };
    }

    private static BattleSkillEffect WithAuraDamageBonus(BattleSkillEffect source, int aura) => new()
    {
        EffectTypeCode = source.EffectTypeCode,
        TargetTypeCode = source.TargetTypeCode,
        DamageSchoolCode = source.DamageSchoolCode,
        BaseValue = source.BaseValue,
        DurationTurns = source.DurationTurns,
        ChancePercent = source.ChancePercent,
        MaxStacks = source.MaxStacks,
        DisplayOrder = source.DisplayOrder,
        ExecutionGroup = source.ExecutionGroup,
        ConditionCode = source.ConditionCode,
        Scalings = source.Scalings,
        StatModifiers = source.StatModifiers,
        Parameters = BuildParametersWithAuraDamageBonus(source, aura)
    };

    private static Dictionary<string, BattleSkillEffectParameter> BuildParametersWithAuraDamageBonus(
        BattleSkillEffect source,
        int aura)
    {
        var parameters = new Dictionary<string, BattleSkillEffectParameter>(source.Parameters, StringComparer.OrdinalIgnoreCase);
        var bonusPerPoint = source.GetDecimal(
            "AURA_DAMAGE_PERCENT_PER_POINT",
            ThanhThaiAuraResourceHandler.DefaultDamageBonusPercentPerAura);
        var existingBonus = source.GetDecimal("DAMAGE_BONUS_PERCENT", 0m);
        parameters["DAMAGE_BONUS_PERCENT"] = new(
            "DAMAGE_BONUS_PERCENT",
            existingBonus + aura * bonusPerPoint,
            null, null, null);
        parameters.Remove("SNAPSHOT_AURA_VALUE");
        parameters.Remove("IGNORE_AURA_DAMAGE_BONUS");
        return parameters;
    }

    private static PendingBattleEvent Stamp(
        PendingBattleEvent evt,
        string executionGroup,
        int hitIndex,
        int timelineOffsetMs,
        string phaseCode) => new()
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
        ExecutionGroup = executionGroup,
        HitIndex = hitIndex,
        TimelineOffsetMs = timelineOffsetMs,
        PhaseCode = phaseCode,
        StatModifiers = evt.StatModifiers,
        ResourceCode = evt.ResourceCode,
        PreviousValue = evt.PreviousValue,
        CurrentValue = evt.CurrentValue,
        ReasonCode = evt.ReasonCode,
        ActionId = evt.ActionId
    };
}
