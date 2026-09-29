using GAME.Domain.Battle.Effects;
using GAME.Domain.Battle.Targets;

namespace GAME.Domain.Battle.Skills.SibaThienThan;

public sealed class SibaThienThanSkillHandler : ISkillHandler
{
    private readonly DefaultSkillHandler _defaultHandler;
    private readonly BattleEffectHandlerRegistry _effectHandlers;
    private readonly BattleTargetSelectorRegistry _targetSelectors;
    private readonly HealEffectHandler _healHandler;
    private readonly EnergyChangeEffectHandler _energyHandler;
    private readonly DispelDebuffEffectHandler _dispelHandler;

    public SibaThienThanSkillHandler(
        DefaultSkillHandler defaultHandler,
        BattleEffectHandlerRegistry effectHandlers,
        BattleTargetSelectorRegistry? targetSelectors = null)
    {
        _defaultHandler = defaultHandler;
        _effectHandlers = effectHandlers;
        _targetSelectors = targetSelectors ?? BattleTargetSelectorRegistry.CreateDefault();
        _healHandler = (effectHandlers.CanHandle(BattleCodes.Heal) ? effectHandlers.GetRequired(BattleCodes.Heal) as HealEffectHandler : null) ?? new HealEffectHandler();
        _energyHandler = (effectHandlers.CanHandle(BattleCodes.EnergyChange) ? effectHandlers.GetRequired(BattleCodes.EnergyChange) as EnergyChangeEffectHandler : null) ?? new EnergyChangeEffectHandler();
        _dispelHandler = (effectHandlers.CanHandle(BattleCodes.DispelDebuff) ? effectHandlers.GetRequired(BattleCodes.DispelDebuff) as DispelDebuffEffectHandler : null) ?? new DispelDebuffEffectHandler();
    }

    public static SibaThienThanSkillHandler Create(
        DefaultSkillHandler defaultHandler,
        BattleEffectHandlerRegistry effectHandlers,
        BattleTargetSelectorRegistry? targetSelectors = null) =>
        new(defaultHandler, effectHandlers, targetSelectors);

    public bool CanHandle(BattleSkill skill) =>
        skill.Id.Equals(SibaThienThanSkillCodes.Basic, StringComparison.OrdinalIgnoreCase) ||
        skill.Id.Equals(SibaThienThanSkillCodes.Ultimate, StringComparison.OrdinalIgnoreCase);

    public SkillExecutionResult Execute(SkillExecutionContext context) =>
        context.Skill.Id.Equals(SibaThienThanSkillCodes.Basic, StringComparison.OrdinalIgnoreCase)
            ? ExecuteBasic(context)
            : ExecuteUltimate(context);

    public SkillExecutionResult ExecuteBasic(SkillExecutionContext context)
    {
        var result = new SkillExecutionResult();
        var effects = context.Skill.Effects.OrderBy(x => x.DisplayOrder).ToList();
        var primaryEffect = effects.FirstOrDefault() ??
            throw new InvalidOperationException($"Skill '{context.Skill.Id}' has no effects configured.");

        // 1. Resolve Target (single living ally, prefers missing Encouragement, lowest % HP)
        var target = _defaultHandler.ResolveTargets(primaryEffect.TargetTypeCode, context, primaryEffect).FirstOrDefault();
        if (target == null || !target.IsAlive) return result;

        result.TargetedCombatantIds.Add(target.Id);

        // 2. Heal target (140% Magic Damage, no crit)
        var healEffect = effects.FirstOrDefault(x => x.EffectTypeCode.Equals(BattleCodes.Heal, StringComparison.OrdinalIgnoreCase)) ?? primaryEffect;
        var healAmount = Math.Max(1, (int)Math.Round(BattleStatCalculator.CalculateEffectValue(healEffect, context.Actor)));
        var hpBefore = target.Hp;
        target.Hp = Math.Min(target.MaxHp, target.Hp + healAmount);
        var actualHeal = target.Hp - hpBefore;

        result.Events.Add(new PendingBattleEvent
        {
            EventType = "HEAL",
            ActorId = context.Actor.Id,
            TargetId = target.Id,
            SkillId = context.Skill.Id,
            EffectTypeCode = BattleCodes.Heal,
            Value = actualHeal,
            HpBefore = hpBefore,
            HpAfter = target.Hp,
            TimelineOffsetMs = 480,
            PhaseCode = "IMPACT"
        });

        // 3. Grant Energy (+10 Energy, clamped)
        var energyEffect = effects.FirstOrDefault(x => x.EffectTypeCode.Equals(BattleCodes.EnergyChange, StringComparison.OrdinalIgnoreCase));
        var energyDelta = energyEffect?.GetInt(SibaThienThanSkillCodes.ParamEnergyDelta, 10)
            ?? energyEffect?.GetInt(SibaThienThanSkillCodes.ParamEnergyGain, 10)
            ?? 10;

        var energyBefore = target.Energy;
        target.Energy = Math.Clamp(target.Energy + energyDelta, 0, target.MaxEnergy);

        result.Events.Add(new PendingBattleEvent
        {
            EventType = BattleCodes.EnergyChanged,
            ActorId = context.Actor.Id,
            TargetId = target.Id,
            SkillId = context.Skill.Id,
            EffectTypeCode = BattleCodes.EnergyChange,
            Value = target.Energy - energyBefore,
            EnergyBefore = energyBefore,
            EnergyAfter = target.Energy,
            TimelineOffsetMs = 620,
            PhaseCode = "IMPACT"
        });

        // 4. Roll Encouragement (50% chance, 2 turns)
        var encEffect = effects.FirstOrDefault(x =>
            x.EffectTypeCode.Equals(BattleCodes.StatBuff, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(x.GetString(SibaThienThanSkillCodes.ParamStatusGroup), SibaThienThanSkillCodes.StatusGroupEncouragement, StringComparison.OrdinalIgnoreCase))
            ?? effects.ElementAtOrDefault(2);

        var chancePercent = encEffect?.ChancePercent ?? 50m;
        var durationTurns = encEffect?.GetInt(SibaThienThanSkillCodes.ParamDurationTurns, 2) ?? 2;
        var buffPercent = encEffect?.GetInt(SibaThienThanSkillCodes.ParamBuffPercent, 20) ?? (encEffect?.BaseValue != 0 ? encEffect?.BaseValue ?? 20m : 20m);

        if (context.Random.Next(1, 101) <= (int)Math.Round(chancePercent))
        {
            // Back row (2, 4): OFFENSE (+20% ATK, +20% MAGIC_DAMAGE)
            // Front row (1, 3, 5): DEFENSE (+20% DEF, +20% MAGIC_RESISTANCE)
            bool isBackRow = target.Position == 2 || target.Position == 4;
            string variant = isBackRow ? SibaThienThanSkillCodes.StatusOffense : SibaThienThanSkillCodes.StatusDefense;
            string opposite = isBackRow ? SibaThienThanSkillCodes.StatusDefense : SibaThienThanSkillCodes.StatusOffense;

            // Remove opposite variant if present (mutually exclusive)
            var removedOpposite = target.StatusEffects.RemoveAll(x =>
                x.EffectTypeCode.Equals(opposite, StringComparison.OrdinalIgnoreCase) ||
                (!string.IsNullOrEmpty(x.StatusGroup) &&
                 x.StatusGroup.Equals(SibaThienThanSkillCodes.StatusGroupEncouragement, StringComparison.OrdinalIgnoreCase) &&
                 !x.EffectTypeCode.Equals(variant, StringComparison.OrdinalIgnoreCase)));

            if (removedOpposite > 0)
            {
                result.Events.Add(new PendingBattleEvent
                {
                    EventType = BattleCodes.StatusRemoved,
                    ActorId = context.Actor.Id,
                    TargetId = target.Id,
                    SkillId = context.Skill.Id,
                    EffectTypeCode = opposite,
                    TimelineOffsetMs = 750,
                    PhaseCode = "IMPACT"
                });
            }

            var existing = target.StatusEffects.FirstOrDefault(x =>
                x.EffectTypeCode.Equals(variant, StringComparison.OrdinalIgnoreCase));

            var statMods = BuildEncouragementModifiers(variant, buffPercent);
            var instanceId = $"{context.Actor.Id}:{context.Skill.Id}:{variant}:{target.Id}";

            if (existing != null)
            {
                existing.RemainingTurns = durationTurns;
                result.Events.Add(new PendingBattleEvent
                {
                    EventType = BattleCodes.StatusRefreshed,
                    ActorId = context.Actor.Id,
                    TargetId = target.Id,
                    SkillId = context.Skill.Id,
                    EffectTypeCode = variant,
                    Value = (int)Math.Round(buffPercent),
                    RemainingTurns = durationTurns,
                    StatModifiers = existing.StatModifiers.Count > 0 ? existing.StatModifiers : statMods,
                    TimelineOffsetMs = 760,
                    PhaseCode = "IMPACT",
                    StatusInstanceId = existing.InstanceId
                });
            }
            else
            {
                var newStatus = new BattleStatusEffect
                {
                    InstanceId = instanceId,
                    EffectTypeCode = variant,
                    StatusGroup = SibaThienThanSkillCodes.StatusGroupEncouragement,
                    SourceSkillId = context.Skill.Id,
                    SourceHeroId = context.Actor.Id,
                    RemainingTurns = durationTurns,
                    AppliedTurn = context.Turn,
                    MaxStacks = 1,
                    Stacks = 1,
                    Value = buffPercent,
                    Dispellable = true,
                    StatModifiers = statMods
                };
                target.StatusEffects.Add(newStatus);

                result.Events.Add(new PendingBattleEvent
                {
                    EventType = "STATUS_APPLIED",
                    ActorId = context.Actor.Id,
                    TargetId = target.Id,
                    SkillId = context.Skill.Id,
                    EffectTypeCode = variant,
                    Value = (int)Math.Round(buffPercent),
                    RemainingTurns = durationTurns,
                    StatModifiers = newStatus.StatModifiers,
                    TimelineOffsetMs = 760,
                    PhaseCode = "IMPACT",
                    StatusInstanceId = instanceId
                });
            }
        }

        // 5. Gain 1 Ân Phúc stack (max 5)
        var resourceEffect = effects.FirstOrDefault(x =>
            string.Equals(x.GetString("RESOURCE_CODE"), SibaThienThanSkillCodes.ResourceBlessing, StringComparison.OrdinalIgnoreCase));

        var maxBlessing = resourceEffect?.GetInt(SibaThienThanSkillCodes.ParamMaxBlessingStacks, 5) ?? 5;
        var gainPerBasic = resourceEffect?.GetInt(SibaThienThanSkillCodes.ParamBlessingGainPerBasic, 1) ?? 1;

        var currentBlessing = context.Actor.GetResource(SibaThienThanSkillCodes.ResourceBlessing, 0);
        if (currentBlessing < maxBlessing)
        {
            var newBlessing = Math.Min(maxBlessing, currentBlessing + gainPerBasic);
            context.Actor.SetResource(SibaThienThanSkillCodes.ResourceBlessing, newBlessing);

            result.Events.Add(new PendingBattleEvent
            {
                EventType = BattleCodes.ResourceChanged,
                ActorId = context.Actor.Id,
                TargetId = context.Actor.Id,
                SkillId = context.Skill.Id,
                ResourceCode = SibaThienThanSkillCodes.ResourceBlessing,
                Value = newBlessing - currentBlessing,
                PreviousValue = currentBlessing,
                CurrentValue = newBlessing,
                TimelineOffsetMs = 900,
                PhaseCode = "IMPACT"
            });
        }

        return result;
    }

    public SkillExecutionResult ExecuteUltimate(SkillExecutionContext context)
    {
        var result = new SkillExecutionResult();
        var effects = context.Skill.Effects.OrderBy(x => x.DisplayOrder).ToList();

        // 1. Snapshot:
        // - Ân Phúc count
        // - Living allies
        // - Living allies having Encouragement
        var currentBlessing = context.Actor.GetResource(SibaThienThanSkillCodes.ResourceBlessing, 0);
        var thresholdEffect = effects.FirstOrDefault(e =>
            string.Equals(e.GetString("RESOURCE_CODE"), SibaThienThanSkillCodes.ResourceBlessing, StringComparison.OrdinalIgnoreCase));

        var blessingCost = thresholdEffect?.GetInt(SibaThienThanSkillCodes.ParamBlessingCostForEmpowered, 5) ?? 5;
        var isEmpowered = currentBlessing >= blessingCost;

        var livingAllies = context.Combatants
            .Where(c => c.Team == context.Actor.Team && c.IsAlive)
            .OrderBy(c => c.Position)
            .ThenBy(c => c.Id)
            .ToList();

        var alliesWithEncouragement = livingAllies
            .Where(a => a.StatusEffects.Any(s =>
                (s.RemainingTurns > 0 || s.RemainingTurns == -1) &&
                (s.EffectTypeCode.Equals(SibaThienThanSkillCodes.StatusOffense, StringComparison.OrdinalIgnoreCase) ||
                 s.EffectTypeCode.Equals(SibaThienThanSkillCodes.StatusDefense, StringComparison.OrdinalIgnoreCase) ||
                 (!string.IsNullOrEmpty(s.StatusGroup) && s.StatusGroup.Equals(SibaThienThanSkillCodes.StatusGroupEncouragement, StringComparison.OrdinalIgnoreCase)))))
            .ToList();

        result.TargetedCombatantIds.AddRange(livingAllies.Select(a => a.Id));

        // If empowered, emit announcement event at start
        if (isEmpowered)
        {
            result.Events.Add(new PendingBattleEvent
            {
                EventType = SibaThienThanSkillCodes.EmpoweredCast,
                ActorId = context.Actor.Id,
                TargetId = context.Actor.Id,
                SkillId = context.Skill.Id,
                Value = currentBlessing,
                TimelineOffsetMs = 0,
                PhaseCode = "CAST"
            });
        }

        // 2. Apply CELESTIAL_PROTECTION (+20% SPD, +20% Resistance, 2 turns) to each living ally
        var celestialEffect = effects.FirstOrDefault(e =>
            e.EffectTypeCode.Equals(BattleCodes.CelestialProtection, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(e.GetString("STATUS_CODE"), SibaThienThanSkillCodes.StatusCelestialProtection, StringComparison.OrdinalIgnoreCase));

        var cpDuration = celestialEffect?.DurationTurns ?? 2;
        var cpStatMods = celestialEffect?.StatModifiers.Count > 0
            ? celestialEffect.StatModifiers
            : new List<BattleStatModifier>
            {
                new("SPD", "PERCENT", 20m),
                new("RESISTANCE", "PERCENT", 20m)
            };

        foreach (var ally in livingAllies)
        {
            var existingCp = ally.StatusEffects.FirstOrDefault(s =>
                s.EffectTypeCode.Equals(SibaThienThanSkillCodes.StatusCelestialProtection, StringComparison.OrdinalIgnoreCase));

            var instanceId = $"{context.Actor.Id}:{context.Skill.Id}:{SibaThienThanSkillCodes.StatusCelestialProtection}:{ally.Id}";
            if (existingCp != null)
            {
                existingCp.RemainingTurns = cpDuration;
                result.Events.Add(new PendingBattleEvent
                {
                    EventType = BattleCodes.StatusRefreshed,
                    ActorId = context.Actor.Id,
                    TargetId = ally.Id,
                    SkillId = context.Skill.Id,
                    EffectTypeCode = SibaThienThanSkillCodes.StatusCelestialProtection,
                    Value = 20,
                    RemainingTurns = cpDuration,
                    StatModifiers = existingCp.StatModifiers.Count > 0 ? existingCp.StatModifiers : cpStatMods,
                    TimelineOffsetMs = 700,
                    PhaseCode = "IMPACT",
                    StatusInstanceId = existingCp.InstanceId
                });
            }
            else
            {
                var newCp = new BattleStatusEffect
                {
                    InstanceId = instanceId,
                    EffectTypeCode = SibaThienThanSkillCodes.StatusCelestialProtection,
                    SourceSkillId = context.Skill.Id,
                    SourceHeroId = context.Actor.Id,
                    RemainingTurns = cpDuration,
                    AppliedTurn = context.Turn,
                    MaxStacks = 1,
                    Stacks = 1,
                    Value = 20m,
                    Dispellable = true,
                    StatModifiers = cpStatMods
                };
                ally.StatusEffects.Add(newCp);

                result.Events.Add(new PendingBattleEvent
                {
                    EventType = "STATUS_APPLIED",
                    ActorId = context.Actor.Id,
                    TargetId = ally.Id,
                    SkillId = context.Skill.Id,
                    EffectTypeCode = SibaThienThanSkillCodes.StatusCelestialProtection,
                    Value = 20,
                    RemainingTurns = cpDuration,
                    StatModifiers = cpStatMods,
                    TimelineOffsetMs = 700,
                    PhaseCode = "IMPACT",
                    StatusInstanceId = instanceId
                });
            }
        }

        // 3. Dispel up to 1 random dispellable debuff per living ally
        var dispelEffect = effects.FirstOrDefault(e =>
            e.EffectTypeCode.Equals(BattleCodes.DispelDebuff, StringComparison.OrdinalIgnoreCase));

        if (dispelEffect != null)
        {
            foreach (var ally in livingAllies)
            {
                var dispelCtx = new BattleEffectContext
                {
                    Effect = dispelEffect,
                    Skill = context.Skill,
                    Actor = context.Actor,
                    Target = ally,
                    SelectedTargets = livingAllies,
                    Combatants = context.Combatants,
                    Random = context.Random,
                    Round = context.Round,
                    Turn = context.Turn,
                    ActionId = context.ActionId,
                    TimelineOffsetMs = 1150,
                    PhaseCode = "IMPACT"
                };

                var dispelEvents = _dispelHandler.Apply(dispelCtx);
                foreach (var evt in dispelEvents)
                {
                    result.Events.Add(new PendingBattleEvent
                    {
                        EventType = evt.EventType,
                        ActorId = context.Actor.Id,
                        TargetId = ally.Id,
                        SkillId = context.Skill.Id,
                        EffectTypeCode = evt.EffectTypeCode,
                        StatusInstanceId = evt.StatusInstanceId,
                        ReasonCode = evt.ReasonCode ?? "DISPEL",
                        TimelineOffsetMs = 1150,
                        PhaseCode = "IMPACT"
                    });
                }
            }
        }

        // 4. Empowered branch (if was snapshot with >= 5 stacks)
        if (isEmpowered)
        {

            // 4a. Heal all living allies: 110% Magic Damage of Siba
            var empoweredHealEffect = effects.FirstOrDefault(e =>
                e.EffectTypeCode.Equals(BattleCodes.Heal, StringComparison.OrdinalIgnoreCase) &&
                (string.Equals(e.ExecutionGroup, "EMPOWERED", StringComparison.OrdinalIgnoreCase) || e.DisplayOrder == 4));

            var healCoeff = empoweredHealEffect?.Scalings.FirstOrDefault(x =>
                x.AttributeCode.Equals("MAGIC_DAMAGE", StringComparison.OrdinalIgnoreCase))?.Coefficient ?? 1.10m;

            var baseHeal = empoweredHealEffect != null
                ? BattleStatCalculator.CalculateEffectValue(empoweredHealEffect, context.Actor)
                : context.Actor.MagicDamage * healCoeff;

            var healAmount = Math.Max(1, (int)Math.Round(baseHeal));

            foreach (var ally in livingAllies)
            {
                var hpBefore = ally.Hp;
                ally.Hp = Math.Min(ally.MaxHp, ally.Hp + healAmount);
                var actualHeal = ally.Hp - hpBefore;

                result.Events.Add(new PendingBattleEvent
                {
                    EventType = "HEAL",
                    ActorId = context.Actor.Id,
                    TargetId = ally.Id,
                    SkillId = context.Skill.Id,
                    EffectTypeCode = BattleCodes.Heal,
                    Value = actualHeal,
                    HpBefore = hpBefore,
                    HpAfter = ally.Hp,
                    TimelineOffsetMs = 1450,
                    PhaseCode = "IMPACT",
                    ExecutionGroup = "EMPOWERED"
                });
            }

            // 4b & 4c. Apply Encouragement to the whole living team. Allies who already
            // had Encouragement in the pre-cast snapshot keep their variant, refresh it,
            // and gain Energy. Newly encouraged allies get the variant for their current row.
            var empoweredEncEffect = effects.FirstOrDefault(e =>
                (string.Equals(e.ExecutionGroup, "EMPOWERED", StringComparison.OrdinalIgnoreCase) || e.DisplayOrder == 5) &&
                string.Equals(e.GetString(SibaThienThanSkillCodes.ParamStatusGroup), SibaThienThanSkillCodes.StatusGroupEncouragement, StringComparison.OrdinalIgnoreCase));

            var refreshDuration = empoweredEncEffect?.GetInt(SibaThienThanSkillCodes.ParamRefreshDuration, 2) ?? 2;
            var empEnergyGain = empoweredEncEffect?.GetInt(SibaThienThanSkillCodes.ParamEmpoweredEnergyGain, 15) ?? 15;
            var configuredBuffValue = empoweredEncEffect?.BaseValue ?? 0m;
            var buffPercent = empoweredEncEffect?.GetInt(SibaThienThanSkillCodes.ParamBuffPercent, 20)
                ?? (configuredBuffValue != 0m ? configuredBuffValue : 20m);
            var encouragementSnapshotIds = alliesWithEncouragement.Select(x => x.Id).ToHashSet();

            foreach (var ally in livingAllies)
            {
                var encStatus = ally.StatusEffects.FirstOrDefault(s =>
                    (s.RemainingTurns > 0 || s.RemainingTurns == -1) &&
                    (s.EffectTypeCode.Equals(SibaThienThanSkillCodes.StatusOffense, StringComparison.OrdinalIgnoreCase) ||
                     s.EffectTypeCode.Equals(SibaThienThanSkillCodes.StatusDefense, StringComparison.OrdinalIgnoreCase) ||
                     (!string.IsNullOrEmpty(s.StatusGroup) && s.StatusGroup.Equals(SibaThienThanSkillCodes.StatusGroupEncouragement, StringComparison.OrdinalIgnoreCase))));

                if (encouragementSnapshotIds.Contains(ally.Id) && encStatus != null)
                {
                    encStatus.RemainingTurns = refreshDuration; // Exactly 2 turns
                    result.Events.Add(new PendingBattleEvent
                    {
                        EventType = BattleCodes.StatusRefreshed,
                        ActorId = context.Actor.Id,
                        TargetId = ally.Id,
                        SkillId = context.Skill.Id,
                        EffectTypeCode = encStatus.EffectTypeCode,
                        RemainingTurns = refreshDuration,
                        StatModifiers = encStatus.StatModifiers,
                        TimelineOffsetMs = 1600,
                        PhaseCode = "IMPACT",
                        StatusInstanceId = encStatus.InstanceId,
                        ExecutionGroup = "EMPOWERED"
                    });

                    // Only allies who had Encouragement before this cast gain Energy.
                    var eBefore = ally.Energy;
                    ally.Energy = Math.Clamp(ally.Energy + empEnergyGain, 0, ally.MaxEnergy);

                    result.Events.Add(new PendingBattleEvent
                    {
                        EventType = BattleCodes.EnergyChanged,
                        ActorId = context.Actor.Id,
                        TargetId = ally.Id,
                        SkillId = context.Skill.Id,
                        EffectTypeCode = BattleCodes.EnergyChange,
                        Value = ally.Energy - eBefore,
                        EnergyBefore = eBefore,
                        EnergyAfter = ally.Energy,
                        TimelineOffsetMs = 1900,
                        PhaseCode = "IMPACT",
                        ExecutionGroup = "EMPOWERED"
                    });
                }
                else
                {
                    var isBackRow = ally.Position is 2 or 4;
                    var variant = isBackRow
                        ? SibaThienThanSkillCodes.StatusOffense
                        : SibaThienThanSkillCodes.StatusDefense;
                    var statMods = BuildEncouragementModifiers(variant, buffPercent);
                    var instanceId = $"{context.Actor.Id}:{context.Skill.Id}:{variant}:{ally.Id}";

                    // Remove expired or malformed members of the mutually-exclusive group
                    // before applying the row-appropriate variant.
                    ally.StatusEffects.RemoveAll(s =>
                        s.EffectTypeCode.Equals(SibaThienThanSkillCodes.StatusOffense, StringComparison.OrdinalIgnoreCase) ||
                        s.EffectTypeCode.Equals(SibaThienThanSkillCodes.StatusDefense, StringComparison.OrdinalIgnoreCase) ||
                        (!string.IsNullOrEmpty(s.StatusGroup) &&
                         s.StatusGroup.Equals(SibaThienThanSkillCodes.StatusGroupEncouragement, StringComparison.OrdinalIgnoreCase)));

                    var newStatus = new BattleStatusEffect
                    {
                        InstanceId = instanceId,
                        EffectTypeCode = variant,
                        StatusGroup = SibaThienThanSkillCodes.StatusGroupEncouragement,
                        SourceSkillId = context.Skill.Id,
                        SourceHeroId = context.Actor.Id,
                        RemainingTurns = refreshDuration,
                        AppliedTurn = context.Turn,
                        MaxStacks = 1,
                        Stacks = 1,
                        Value = buffPercent,
                        Dispellable = true,
                        StatModifiers = statMods
                    };
                    ally.StatusEffects.Add(newStatus);

                    result.Events.Add(new PendingBattleEvent
                    {
                        EventType = "STATUS_APPLIED",
                        ActorId = context.Actor.Id,
                        TargetId = ally.Id,
                        SkillId = context.Skill.Id,
                        EffectTypeCode = variant,
                        Value = (int)Math.Round(buffPercent),
                        RemainingTurns = refreshDuration,
                        StatModifiers = newStatus.StatModifiers,
                        TimelineOffsetMs = 1600,
                        PhaseCode = "IMPACT",
                        StatusInstanceId = instanceId,
                        ExecutionGroup = "EMPOWERED"
                    });
                }
            }

            // 5. Consume 5 Ân Phúc stacks
            var remainingBlessing = Math.Max(0, currentBlessing - blessingCost);
            context.Actor.SetResource(SibaThienThanSkillCodes.ResourceBlessing, remainingBlessing);

            result.Events.Add(new PendingBattleEvent
            {
                EventType = BattleCodes.ResourceChanged,
                ActorId = context.Actor.Id,
                TargetId = context.Actor.Id,
                SkillId = context.Skill.Id,
                ResourceCode = SibaThienThanSkillCodes.ResourceBlessing,
                Value = -blessingCost,
                PreviousValue = currentBlessing,
                CurrentValue = remainingBlessing,
                TimelineOffsetMs = 1950,
                PhaseCode = "IMPACT",
                ExecutionGroup = "EMPOWERED"
            });
        }

        return result;
    }

    private static IReadOnlyList<BattleStatModifier> BuildEncouragementModifiers(string variant, decimal buffPercent)
    {
        if (variant.Equals(SibaThienThanSkillCodes.StatusOffense, StringComparison.OrdinalIgnoreCase))
        {
            return
            [
                new BattleStatModifier("ATK", "PERCENT", buffPercent),
                new BattleStatModifier("MAGIC_DAMAGE", "PERCENT", buffPercent)
            ];
        }

        return
        [
            new BattleStatModifier("DEF", "PERCENT", buffPercent),
            new BattleStatModifier("MAGIC_RESISTANCE", "PERCENT", buffPercent)
        ];
    }
}
