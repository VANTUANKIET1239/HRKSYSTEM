using GAME.Domain.Battle.Effects;
using GAME.Domain.Battle.Targets;

namespace GAME.Domain.Battle.Skills.NghiaPhucPrime;

public sealed class NghiaPhucPrimeSkillHandler : ISkillHandler
{
    private readonly DefaultSkillHandler _defaultHandler;
    private readonly BattleEffectHandlerRegistry _effectHandlers;
    private readonly DamageEffectHandler _damageHandler;
    private readonly BattleTargetSelectorRegistry _targetSelectors;

    public NghiaPhucPrimeSkillHandler(
        DefaultSkillHandler defaultHandler,
        BattleEffectHandlerRegistry effectHandlers,
        BattleTargetSelectorRegistry? targetSelectors = null)
    {
        _defaultHandler = defaultHandler;
        _effectHandlers = effectHandlers;
        _damageHandler = (effectHandlers.GetRequired(BattleCodes.Damage) as DamageEffectHandler) ?? new DamageEffectHandler();
        _targetSelectors = targetSelectors ?? BattleTargetSelectorRegistry.CreateDefault();
    }

    public static NghiaPhucPrimeSkillHandler Create(
        DefaultSkillHandler defaultHandler,
        BattleEffectHandlerRegistry effectHandlers,
        BattleTargetSelectorRegistry? targetSelectors = null) =>
        new(defaultHandler, effectHandlers, targetSelectors);

    public bool CanHandle(BattleSkill skill) =>
        skill.Id.Equals(NghiaPhucPrimeSkillCodes.Basic, StringComparison.OrdinalIgnoreCase) ||
        skill.Id.Equals(NghiaPhucPrimeSkillCodes.Ultimate, StringComparison.OrdinalIgnoreCase);

    public SkillExecutionResult Execute(SkillExecutionContext context) =>
        context.Skill.Id.Equals(NghiaPhucPrimeSkillCodes.Basic, StringComparison.OrdinalIgnoreCase)
            ? ExecuteBasic(context)
            : ExecuteUltimate(context);

    public SkillExecutionResult ExecuteBasic(SkillExecutionContext context)
    {
        var result = new SkillExecutionResult();
        var effects = context.Skill.Effects.OrderBy(x => x.DisplayOrder).ToList();
        var primaryEffect = effects.FirstOrDefault() ??
            throw new InvalidOperationException($"Skill '{context.Skill.Id}' has no effects configured.");

        // Target: resolve single enemy target
        var target = _defaultHandler.ResolveTargets(primaryEffect.TargetTypeCode, context).FirstOrDefault();
        if (target == null) return result;

        result.TargetedCombatantIds.Add(target.Id);

        // Parameters from DB effect parameters
        var damageCoeff = primaryEffect.GetDecimal(NghiaPhucPrimeSkillCodes.ParamDamageCoefficient, 0.90m);
        var allyShieldPercent = primaryEffect.GetDecimal(NghiaPhucPrimeSkillCodes.ParamAllyShieldMaxHpPercent, 8m);
        var existingRestorePercent = primaryEffect.GetDecimal(NghiaPhucPrimeSkillCodes.ParamExistingShieldRestorePercent, 4m);
        var shieldDuration = primaryEffect.GetInt(NghiaPhucPrimeSkillCodes.ParamShieldDurationTurns, 2);
        var fortitudeMaxStacks = primaryEffect.GetInt(NghiaPhucPrimeSkillCodes.ParamFortitudeMaxStacks, 4);
        var fortitudeDefPercent = primaryEffect.GetDecimal(NghiaPhucPrimeSkillCodes.ParamFortitudeDefPercentPerStack, 5m);
        var fortitudeMrPercent = primaryEffect.GetDecimal(NghiaPhucPrimeSkillCodes.ParamFortitudeMagicResistancePercentPerStack, 5m);
        var selfShieldPercent = primaryEffect.GetDecimal(NghiaPhucPrimeSkillCodes.ParamSelfShieldOnMaxStackPercent, 12m);
        var canCrit = primaryEffect.GetBool(NghiaPhucPrimeSkillCodes.ParamCanCrit, true);

        // Build damage effect
        var dmgEffect = new BattleSkillEffect
        {
            EffectTypeCode = BattleCodes.Damage,
            DamageSchoolCode = BattleCodes.Physical,
            TargetTypeCode = primaryEffect.TargetTypeCode,
            DurationTurns = 0,
            BaseValue = 0,
            Scalings = [new BattleEffectScaling("ATK", damageCoeff, 0m)],
            Parameters = new Dictionary<string, BattleSkillEffectParameter>(primaryEffect.Parameters, StringComparer.OrdinalIgnoreCase)
            {
                ["CAN_CRIT"] = new("CAN_CRIT", null, null, canCrit, null)
            }
        };

        var dmgContext = new BattleEffectContext
        {
            Effect = dmgEffect,
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
            result.Events.Add(Stamp(evt, 800, "IMPACT"));
        }

        if (dmgResult.WasHit)
        {
            result.BasicAttackHitTargetIds.Add(target.Id);
        }
        if (dmgResult.WasKilled && !result.DefeatedTargetIds.Contains(target.Id))
        {
            result.DefeatedTargetIds.Add(target.Id);
        }

        // Secondary effects only activate if damage connected successfully
        if (dmgResult.WasHit)
        {
            // 1. Pick lowest HP% alive ally (can be Nghia Phuc Prime himself)
            var lowestHpAlly = context.Combatants
                .Where(c => c.Team == context.Actor.Team && c.IsAlive)
                .OrderBy(c => (decimal)c.Hp / c.MaxHp)
                .ThenBy(c => c.Hp)
                .ThenBy(c => c.Position)
                .FirstOrDefault();

            if (lowestHpAlly != null)
            {
                var shieldValue = (int)Math.Round(context.Actor.MaxHp * allyShieldPercent / 100m);
                var restoreValue = (int)Math.Round(context.Actor.MaxHp * existingRestorePercent / 100m);
                var shieldInstanceId = $"{lowestHpAlly.Id}:{NghiaPhucPrimeSkillCodes.ShieldSourceBasic}";

                var existingShield = lowestHpAlly.StatusEffects.FirstOrDefault(s =>
                    s.InstanceId == shieldInstanceId && s.RemainingTurns > 0);

                if (existingShield != null)
                {
                    // Restore shield and refresh duration to 2 turns
                    var maxCap = (int)Math.Round(context.Actor.MaxHp * 25m / 100m);
                    existingShield.ShieldRemaining = Math.Min(maxCap, existingShield.ShieldRemaining + restoreValue);
                    existingShield.RemainingTurns = shieldDuration;
                    existingShield.AppliedTurn = context.Turn;

                    result.Events.Add(new PendingBattleEvent
                    {
                        EventType = BattleCodes.StatusRefreshed,
                        ActorId = context.Actor.Id,
                        TargetId = lowestHpAlly.Id,
                        SkillId = context.Skill.Id,
                        EffectTypeCode = BattleCodes.Shield,
                        Value = existingShield.ShieldRemaining,
                        RemainingTurns = shieldDuration,
                        StatusInstanceId = existingShield.InstanceId,
                        ActionId = context.ActionId,
                        TimelineOffsetMs = 1200,
                        PhaseCode = "STATUS"
                    });
                }
                else
                {
                    // Create new shield status
                    lowestHpAlly.StatusEffects.Add(new BattleStatusEffect
                    {
                        InstanceId = shieldInstanceId,
                        EffectTypeCode = BattleCodes.Shield,
                        SourceSkillId = context.Skill.Id,
                        SourceHeroId = context.Actor.Id,
                        RemainingTurns = shieldDuration,
                        AppliedTurn = context.Turn,
                        Stacks = 1,
                        MaxStacks = 1,
                        ShieldRemaining = shieldValue,
                        Value = shieldValue
                    });

                    result.Events.Add(new PendingBattleEvent
                    {
                        EventType = "SHIELD_APPLIED",
                        ActorId = context.Actor.Id,
                        TargetId = lowestHpAlly.Id,
                        SkillId = context.Skill.Id,
                        EffectTypeCode = BattleCodes.Shield,
                        Value = shieldValue,
                        RemainingTurns = shieldDuration,
                        StatusInstanceId = shieldInstanceId,
                        ActionId = context.ActionId,
                        TimelineOffsetMs = 1200,
                        PhaseCode = "STATUS"
                    });
                }
            }

            // 2. Prime receives 1 stack of Kiên Cố (PRIME_FORTITUDE)
            var fortitudeInstanceId = $"{context.Actor.Id}:{NghiaPhucPrimeSkillCodes.Fortitude}";
            var existingFortitude = context.Actor.StatusEffects.FirstOrDefault(s =>
                s.EffectTypeCode.Equals(NghiaPhucPrimeSkillCodes.Fortitude, StringComparison.OrdinalIgnoreCase));

            var previousFortitudeStacks = existingFortitude?.Stacks ?? 0;
            var currentFortitudeStacks = Math.Min(fortitudeMaxStacks, previousFortitudeStacks + 1);

            if (existingFortitude == null)
            {
                existingFortitude = new BattleStatusEffect
                {
                    InstanceId = fortitudeInstanceId,
                    EffectTypeCode = NghiaPhucPrimeSkillCodes.Fortitude,
                    SourceSkillId = context.Skill.Id,
                    SourceHeroId = context.Actor.Id,
                    RemainingTurns = -1, // Permanent until 4 stacks consumed
                    AppliedTurn = context.Turn,
                    Stacks = 1,
                    MaxStacks = fortitudeMaxStacks,
                    ScaleModifiersWithStacks = true,
                    StatModifiers =
                    [
                        new BattleStatModifier("DEF", "PERCENT", fortitudeDefPercent),
                        new BattleStatModifier("MAGIC_RESISTANCE", "PERCENT", fortitudeMrPercent)
                    ]
                };
                context.Actor.StatusEffects.Add(existingFortitude);

                result.Events.Add(new PendingBattleEvent
                {
                    EventType = "STATUS_APPLIED",
                    ActorId = context.Actor.Id,
                    TargetId = context.Actor.Id,
                    SkillId = context.Skill.Id,
                    EffectTypeCode = NghiaPhucPrimeSkillCodes.Fortitude,
                    CurrentStacks = 1,
                    MaxStacks = fortitudeMaxStacks,
                    StatModifiers = existingFortitude.StatModifiers,
                    ActionId = context.ActionId,
                    TimelineOffsetMs = 1300,
                    PhaseCode = "STATUS"
                });
            }
            else
            {
                existingFortitude.Stacks = currentFortitudeStacks;
                result.Events.Add(new PendingBattleEvent
                {
                    EventType = BattleCodes.StatusStackChanged,
                    ActorId = context.Actor.Id,
                    TargetId = context.Actor.Id,
                    SkillId = context.Skill.Id,
                    EffectTypeCode = NghiaPhucPrimeSkillCodes.Fortitude,
                    PreviousStacks = previousFortitudeStacks,
                    CurrentStacks = currentFortitudeStacks,
                    MaxStacks = fortitudeMaxStacks,
                    StatModifiers = existingFortitude.StatModifiers,
                    ActionId = context.ActionId,
                    TimelineOffsetMs = 1300,
                    PhaseCode = "STATUS"
                });
            }

            result.Events.Add(new PendingBattleEvent
            {
                EventType = NghiaPhucPrimeSkillCodes.EventFortitudeGained,
                ActorId = context.Actor.Id,
                TargetId = context.Actor.Id,
                SkillId = context.Skill.Id,
                EffectTypeCode = NghiaPhucPrimeSkillCodes.Fortitude,
                PreviousStacks = previousFortitudeStacks,
                CurrentStacks = currentFortitudeStacks,
                MaxStacks = fortitudeMaxStacks,
                Value = currentFortitudeStacks,
                ActionId = context.ActionId,
                TimelineOffsetMs = 1300,
                PhaseCode = "STATUS"
            });

            // 3. When reaching 4 stacks: Grant 12% Max HP self-shield, clear fortitude
            if (currentFortitudeStacks >= fortitudeMaxStacks)
            {
                var selfShieldValue = (int)Math.Round(context.Actor.MaxHp * selfShieldPercent / 100m);
                var selfShieldId = $"{context.Actor.Id}:{NghiaPhucPrimeSkillCodes.ShieldSourceBasic}";
                var selfShield = context.Actor.StatusEffects.FirstOrDefault(s => s.InstanceId == selfShieldId);

                if (selfShield != null)
                {
                    selfShield.ShieldRemaining = Math.Max(selfShield.ShieldRemaining, selfShieldValue);
                    selfShield.RemainingTurns = shieldDuration;
                    selfShield.AppliedTurn = context.Turn;
                }
                else
                {
                    context.Actor.StatusEffects.Add(new BattleStatusEffect
                    {
                        InstanceId = selfShieldId,
                        EffectTypeCode = BattleCodes.Shield,
                        SourceSkillId = context.Skill.Id,
                        SourceHeroId = context.Actor.Id,
                        RemainingTurns = shieldDuration,
                        AppliedTurn = context.Turn,
                        Stacks = 1,
                        MaxStacks = 1,
                        ShieldRemaining = selfShieldValue,
                        Value = selfShieldValue
                    });
                }

                result.Events.Add(new PendingBattleEvent
                {
                    EventType = "SHIELD_APPLIED",
                    ActorId = context.Actor.Id,
                    TargetId = context.Actor.Id,
                    SkillId = context.Skill.Id,
                    EffectTypeCode = BattleCodes.Shield,
                    Value = selfShieldValue,
                    RemainingTurns = shieldDuration,
                    StatusInstanceId = selfShieldId,
                    ActionId = context.ActionId,
                    TimelineOffsetMs = 1400,
                    PhaseCode = "STATUS"
                });

                // Clear all 4 Fortitude stacks
                context.Actor.StatusEffects.Remove(existingFortitude);

                result.Events.Add(new PendingBattleEvent
                {
                    EventType = NghiaPhucPrimeSkillCodes.EventFortitudeConsumed,
                    ActorId = context.Actor.Id,
                    TargetId = context.Actor.Id,
                    SkillId = context.Skill.Id,
                    EffectTypeCode = NghiaPhucPrimeSkillCodes.Fortitude,
                    PreviousStacks = fortitudeMaxStacks,
                    CurrentStacks = 0,
                    MaxStacks = fortitudeMaxStacks,
                    Value = fortitudeMaxStacks,
                    ActionId = context.ActionId,
                    TimelineOffsetMs = 1400,
                    PhaseCode = "STATUS"
                });

                result.Events.Add(new PendingBattleEvent
                {
                    EventType = BattleCodes.StatusRemoved,
                    ActorId = context.Actor.Id,
                    TargetId = context.Actor.Id,
                    SkillId = context.Skill.Id,
                    EffectTypeCode = NghiaPhucPrimeSkillCodes.Fortitude,
                    ActionId = context.ActionId,
                    TimelineOffsetMs = 1400,
                    PhaseCode = "STATUS"
                });
            }
        }

        return result;
    }

    public SkillExecutionResult ExecuteUltimate(SkillExecutionContext context)
    {
        var result = new SkillExecutionResult();
        var effects = context.Skill.Effects.OrderBy(x => x.DisplayOrder).ToList();
        var primaryEffect = effects.FirstOrDefault() ??
            throw new InvalidOperationException($"Skill '{context.Skill.Id}' has no effects configured.");

        // Read parameters from DB effect configuration
        var dmgCoeff = primaryEffect.GetDecimal(NghiaPhucPrimeSkillCodes.ParamFortressDamageCoefficient, 0.60m);
        var staggerDuration = primaryEffect.GetInt(NghiaPhucPrimeSkillCodes.ParamStaggerDurationTurns, 1);
        var abReduction = primaryEffect.GetInt(NghiaPhucPrimeSkillCodes.ParamActionBarReduction, 15);
        var spdReductionPercent = primaryEffect.GetDecimal(NghiaPhucPrimeSkillCodes.ParamSpeedReductionPercent, 10m);
        var brokenMoraleTargetCount = primaryEffect.GetInt(NghiaPhucPrimeSkillCodes.ParamBrokenMoraleTargetCount, 3);
        var outgoingReductionPercent = primaryEffect.GetDecimal(NghiaPhucPrimeSkillCodes.ParamOutgoingDamageReductionPercent, 15m);
        var brokenMoraleDuration = primaryEffect.GetInt(NghiaPhucPrimeSkillCodes.ParamBrokenMoraleDurationTurns, 2);
        var teamShieldCasterHpPercent = primaryEffect.GetDecimal(NghiaPhucPrimeSkillCodes.ParamTeamShieldCasterMaxHpPercent, 10m);
        var teamShieldCasterDefPercent = primaryEffect.GetDecimal(NghiaPhucPrimeSkillCodes.ParamTeamShieldCasterDefPercent, 120m);
        var teamShieldCapPercent = primaryEffect.GetDecimal(NghiaPhucPrimeSkillCodes.ParamTeamShieldTargetMaxHpCapPercent, 25m);
        var teamShieldDuration = primaryEffect.GetInt(NghiaPhucPrimeSkillCodes.ParamTeamShieldDurationTurns, 2);
        var guardianDuration = primaryEffect.GetInt(NghiaPhucPrimeSkillCodes.ParamGuardianDurationTurns, 2);

        // 1. Emit PRIME_FORTRESS_CHARGE_STARTED (0ms)
        result.Events.Add(new PendingBattleEvent
        {
            EventType = NghiaPhucPrimeSkillCodes.EventFortressChargeStarted,
            ActorId = context.Actor.Id,
            TargetId = context.Actor.Id,
            SkillId = context.Skill.Id,
            ActionId = context.ActionId,
            TimelineOffsetMs = 0,
            PhaseCode = "CAST"
        });

        // 2. Resolve all alive enemies
        var aliveEnemies = context.Combatants.Where(c => c.Team != context.Actor.Team && c.IsAlive).ToList();
        foreach (var enemy in aliveEnemies)
        {
            result.TargetedCombatantIds.Add(enemy.Id);
        }

        var hitEnemies = new List<BattleCombatant>();

        // AoE Damage: 60% ATK, cannot crit, affected by DEF
        var fortressDmgEffect = new BattleSkillEffect
        {
            EffectTypeCode = BattleCodes.Damage,
            DamageSchoolCode = BattleCodes.Physical,
            TargetTypeCode = BattleCodes.EnemyAll,
            DurationTurns = 0,
            BaseValue = 0,
            Scalings = [new BattleEffectScaling("ATK", dmgCoeff, 0m)],
            Parameters = new Dictionary<string, BattleSkillEffectParameter>(primaryEffect.Parameters, StringComparer.OrdinalIgnoreCase)
            {
                ["CAN_CRIT"] = new("CAN_CRIT", null, null, false, null)
            }
        };

        foreach (var enemy in aliveEnemies)
        {
            var dmgContext = new BattleEffectContext
            {
                Effect = fortressDmgEffect,
                Skill = context.Skill,
                Actor = context.Actor,
                Target = enemy,
                SelectedTargets = aliveEnemies,
                Combatants = context.Combatants,
                Random = context.Random,
                Round = context.Round,
                Turn = context.Turn,
                ActionId = context.ActionId,
                TimelineOffsetMs = 1450,
                PhaseCode = "IMPACT"
            };

            var dmgResult = _damageHandler.ExecuteDamage(dmgContext);
            foreach (var evt in dmgResult.EmittedEvents)
            {
                result.Events.Add(Stamp(evt, 1450, "IMPACT"));
            }

            if (dmgResult.WasHit && enemy.IsAlive)
            {
                hitEnemies.Add(enemy);
            }
            if (dmgResult.WasKilled && !result.DefeatedTargetIds.Contains(enemy.Id))
            {
                result.DefeatedTargetIds.Add(enemy.Id);
            }
        }

        // 3. Emit PRIME_FORTRESS_IMPACT (1450ms)
        result.Events.Add(new PendingBattleEvent
        {
            EventType = NghiaPhucPrimeSkillCodes.EventFortressImpact,
            ActorId = context.Actor.Id,
            TargetId = context.Actor.Id,
            SkillId = context.Skill.Id,
            ActionId = context.ActionId,
            TimelineOffsetMs = 1450,
            PhaseCode = "IMPACT"
        });

        // 4. Apply Lung Lay (PRIME_STAGGER) to all hit enemies
        foreach (var enemy in hitEnemies)
        {
            // Reduce action bar by 15 points (down to min 0)
            var prevEnergy = enemy.Energy;
            var newEnergy = Math.Max(0, enemy.Energy - abReduction);
            enemy.Energy = newEnergy;

            result.Events.Add(new PendingBattleEvent
            {
                EventType = BattleCodes.ActionBarChanged,
                ActorId = context.Actor.Id,
                TargetId = enemy.Id,
                SkillId = context.Skill.Id,
                EffectTypeCode = NghiaPhucPrimeSkillCodes.Stagger,
                Value = newEnergy - prevEnergy,
                PreviousValue = prevEnergy,
                CurrentValue = newEnergy,
                EnergyBefore = prevEnergy,
                EnergyAfter = newEnergy,
                ActionId = context.ActionId,
                TimelineOffsetMs = 1500,
                PhaseCode = "IMPACT"
            });

            // Stagger status with -10% SPD
            var staggerInstanceId = $"{enemy.Id}:{NghiaPhucPrimeSkillCodes.Stagger}";
            var existingStagger = enemy.StatusEffects.FirstOrDefault(s => s.InstanceId == staggerInstanceId);

            if (existingStagger != null)
            {
                existingStagger.RemainingTurns = staggerDuration;
                existingStagger.AppliedTurn = context.Turn;

                result.Events.Add(new PendingBattleEvent
                {
                    EventType = BattleCodes.StatusRefreshed,
                    ActorId = context.Actor.Id,
                    TargetId = enemy.Id,
                    SkillId = context.Skill.Id,
                    EffectTypeCode = NghiaPhucPrimeSkillCodes.Stagger,
                    RemainingTurns = staggerDuration,
                    ActionId = context.ActionId,
                    TimelineOffsetMs = 1500,
                    PhaseCode = "IMPACT"
                });
            }
            else
            {
                enemy.StatusEffects.Add(new BattleStatusEffect
                {
                    InstanceId = staggerInstanceId,
                    EffectTypeCode = NghiaPhucPrimeSkillCodes.Stagger,
                    SourceSkillId = context.Skill.Id,
                    SourceHeroId = context.Actor.Id,
                    RemainingTurns = staggerDuration,
                    AppliedTurn = context.Turn,
                    Stacks = 1,
                    MaxStacks = 1,
                    StatModifiers =
                    [
                        new BattleStatModifier("SPD", "PERCENT", -spdReductionPercent)
                    ]
                });

                result.Events.Add(new PendingBattleEvent
                {
                    EventType = "STATUS_APPLIED",
                    ActorId = context.Actor.Id,
                    TargetId = enemy.Id,
                    SkillId = context.Skill.Id,
                    EffectTypeCode = NghiaPhucPrimeSkillCodes.Stagger,
                    RemainingTurns = staggerDuration,
                    StatModifiers = [new BattleStatModifier("SPD", "PERCENT", -spdReductionPercent)],
                    ActionId = context.ActionId,
                    TimelineOffsetMs = 1500,
                    PhaseCode = "IMPACT"
                });
            }

            result.Events.Add(new PendingBattleEvent
            {
                EventType = NghiaPhucPrimeSkillCodes.EventStaggerApplied,
                ActorId = context.Actor.Id,
                TargetId = enemy.Id,
                SkillId = context.Skill.Id,
                EffectTypeCode = NghiaPhucPrimeSkillCodes.Stagger,
                RemainingTurns = staggerDuration,
                ActionId = context.ActionId,
                TimelineOffsetMs = 1500,
                PhaseCode = "IMPACT"
            });
        }

        // 5. Apply Vỡ Trận (PRIME_BROKEN_MORALE) to up to 3 random distinct alive enemies using context.Random
        var enemiesStillAlive = context.Combatants.Where(c => c.Team != context.Actor.Team && c.IsAlive).ToList();
        var moraleTargets = enemiesStillAlive
            .OrderBy(_ => context.Random.Next())
            .Take(brokenMoraleTargetCount)
            .ToList();

        foreach (var enemy in moraleTargets)
        {
            var moraleInstanceId = $"{enemy.Id}:{NghiaPhucPrimeSkillCodes.BrokenMorale}";
            var existingMorale = enemy.StatusEffects.FirstOrDefault(s => s.InstanceId == moraleInstanceId);

            if (existingMorale != null)
            {
                existingMorale.RemainingTurns = brokenMoraleDuration;
                existingMorale.AppliedTurn = context.Turn;

                result.Events.Add(new PendingBattleEvent
                {
                    EventType = BattleCodes.StatusRefreshed,
                    ActorId = context.Actor.Id,
                    TargetId = enemy.Id,
                    SkillId = context.Skill.Id,
                    EffectTypeCode = NghiaPhucPrimeSkillCodes.BrokenMorale,
                    RemainingTurns = brokenMoraleDuration,
                    ActionId = context.ActionId,
                    TimelineOffsetMs = 1750,
                    PhaseCode = "IMPACT"
                });
            }
            else
            {
                enemy.StatusEffects.Add(new BattleStatusEffect
                {
                    InstanceId = moraleInstanceId,
                    EffectTypeCode = NghiaPhucPrimeSkillCodes.BrokenMorale,
                    SourceSkillId = context.Skill.Id,
                    SourceHeroId = context.Actor.Id,
                    RemainingTurns = brokenMoraleDuration,
                    AppliedTurn = context.Turn,
                    Stacks = 1,
                    MaxStacks = 1,
                    OutgoingDamageBonusPerStackPercent = -outgoingReductionPercent
                });

                result.Events.Add(new PendingBattleEvent
                {
                    EventType = "STATUS_APPLIED",
                    ActorId = context.Actor.Id,
                    TargetId = enemy.Id,
                    SkillId = context.Skill.Id,
                    EffectTypeCode = NghiaPhucPrimeSkillCodes.BrokenMorale,
                    RemainingTurns = brokenMoraleDuration,
                    ActionId = context.ActionId,
                    TimelineOffsetMs = 1750,
                    PhaseCode = "IMPACT"
                });
            }
        }

        // 6. Emit PRIME_FORTRESS_RETURNED (2050ms)
        result.Events.Add(new PendingBattleEvent
        {
            EventType = NghiaPhucPrimeSkillCodes.EventFortressReturned,
            ActorId = context.Actor.Id,
            TargetId = context.Actor.Id,
            SkillId = context.Skill.Id,
            ActionId = context.ActionId,
            TimelineOffsetMs = 2050,
            PhaseCode = "RECOVERY"
        });

        // 7. Team Shield to all alive allies (2450ms)
        var aliveAllies = context.Combatants.Where(c => c.Team == context.Actor.Team && c.IsAlive).ToList();
        var effectiveDef = BattleStatCalculator.GetEffectiveStat(context.Actor, "DEF");
        var rawShield = (context.Actor.MaxHp * teamShieldCasterHpPercent / 100m) +
                        (effectiveDef * teamShieldCasterDefPercent / 100m);

        foreach (var ally in aliveAllies)
        {
            var cap = (int)Math.Round(ally.MaxHp * teamShieldCapPercent / 100m);
            var finalShield = Math.Min((int)Math.Round(rawShield), cap);
            var teamShieldId = $"{ally.Id}:{NghiaPhucPrimeSkillCodes.ShieldSourceTeam}";

            var existingShield = ally.StatusEffects.FirstOrDefault(s => s.InstanceId == teamShieldId && s.RemainingTurns > 0);

            if (existingShield != null)
            {
                existingShield.ShieldRemaining = Math.Max(existingShield.ShieldRemaining, finalShield);
                existingShield.RemainingTurns = teamShieldDuration;
                existingShield.AppliedTurn = context.Turn;

                result.Events.Add(new PendingBattleEvent
                {
                    EventType = BattleCodes.StatusRefreshed,
                    ActorId = context.Actor.Id,
                    TargetId = ally.Id,
                    SkillId = context.Skill.Id,
                    EffectTypeCode = BattleCodes.Shield,
                    Value = existingShield.ShieldRemaining,
                    RemainingTurns = teamShieldDuration,
                    StatusInstanceId = existingShield.InstanceId,
                    ActionId = context.ActionId,
                    TimelineOffsetMs = 2450,
                    PhaseCode = "RECOVERY"
                });
            }
            else
            {
                ally.StatusEffects.Add(new BattleStatusEffect
                {
                    InstanceId = teamShieldId,
                    EffectTypeCode = BattleCodes.Shield,
                    SourceSkillId = context.Skill.Id,
                    SourceHeroId = context.Actor.Id,
                    RemainingTurns = teamShieldDuration,
                    AppliedTurn = context.Turn,
                    Stacks = 1,
                    MaxStacks = 1,
                    ShieldRemaining = finalShield,
                    Value = finalShield
                });

                result.Events.Add(new PendingBattleEvent
                {
                    EventType = "SHIELD_APPLIED",
                    ActorId = context.Actor.Id,
                    TargetId = ally.Id,
                    SkillId = context.Skill.Id,
                    EffectTypeCode = BattleCodes.Shield,
                    Value = finalShield,
                    RemainingTurns = teamShieldDuration,
                    StatusInstanceId = teamShieldId,
                    ActionId = context.ActionId,
                    TimelineOffsetMs = 2450,
                    PhaseCode = "RECOVERY"
                });
            }
        }

        // 8. Apply PRIME_GUARDIAN to Nghia Phuc Prime (2450ms)
        var guardianInstanceId = $"{context.Actor.Id}:{NghiaPhucPrimeSkillCodes.Guardian}";
        var existingGuardian = context.Actor.StatusEffects.FirstOrDefault(s => s.InstanceId == guardianInstanceId);

        if (existingGuardian != null)
        {
            existingGuardian.RemainingTurns = guardianDuration;
            existingGuardian.AppliedTurn = context.Turn;
        }
        else
        {
            context.Actor.StatusEffects.Add(new BattleStatusEffect
            {
                InstanceId = guardianInstanceId,
                EffectTypeCode = NghiaPhucPrimeSkillCodes.Guardian,
                SourceSkillId = context.Skill.Id,
                SourceHeroId = context.Actor.Id,
                RemainingTurns = guardianDuration,
                AppliedTurn = context.Turn,
                Stacks = 1,
                MaxStacks = 1,
                Value = 0
            });
        }

        result.Events.Add(new PendingBattleEvent
        {
            EventType = "STATUS_APPLIED",
            ActorId = context.Actor.Id,
            TargetId = context.Actor.Id,
            SkillId = context.Skill.Id,
            EffectTypeCode = NghiaPhucPrimeSkillCodes.Guardian,
            RemainingTurns = guardianDuration,
            ActionId = context.ActionId,
            TimelineOffsetMs = 2450,
            PhaseCode = "RECOVERY"
        });

        result.Events.Add(new PendingBattleEvent
        {
            EventType = NghiaPhucPrimeSkillCodes.EventGuardianApplied,
            ActorId = context.Actor.Id,
            TargetId = context.Actor.Id,
            SkillId = context.Skill.Id,
            EffectTypeCode = NghiaPhucPrimeSkillCodes.Guardian,
            RemainingTurns = guardianDuration,
            ActionId = context.ActionId,
            TimelineOffsetMs = 2450,
            PhaseCode = "RECOVERY"
        });

        return result;
    }

    private static PendingBattleEvent Stamp(PendingBattleEvent evt, int offsetMs, string phaseCode) => new()
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
        ExecutionGroup = evt.ExecutionGroup,
        HitIndex = evt.HitIndex,
        ResourceCode = evt.ResourceCode,
        PreviousValue = evt.PreviousValue,
        CurrentValue = evt.CurrentValue,
        ReasonCode = evt.ReasonCode,
        ActionId = evt.ActionId,
        StatusInstanceId = evt.StatusInstanceId,
        TimelineOffsetMs = evt.TimelineOffsetMs ?? offsetMs,
        PhaseCode = evt.PhaseCode ?? phaseCode,
        SourceHeroId = evt.SourceHeroId,
        OriginalDamage = evt.OriginalDamage,
        RedirectRequested = evt.RedirectRequested,
        RedirectActual = evt.RedirectActual,
        AllyDamageAfterRedirect = evt.AllyDamageAfterRedirect,
        GuardianHpBefore = evt.GuardianHpBefore,
        GuardianHpAfter = evt.GuardianHpAfter
    };
}
