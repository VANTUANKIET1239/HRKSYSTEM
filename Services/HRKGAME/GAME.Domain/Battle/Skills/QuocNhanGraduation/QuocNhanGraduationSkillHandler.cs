using GAME.Domain.Battle.Effects;
using GAME.Domain.Battle.Targets;

namespace GAME.Domain.Battle.Skills.QuocNhanGraduation;

public sealed class QuocNhanGraduationSkillHandler : ISkillHandler
{
    private readonly DefaultSkillHandler _defaultHandler;
    private readonly DamageEffectHandler _damageHandler;

    public QuocNhanGraduationSkillHandler(
        DefaultSkillHandler defaultHandler,
        BattleEffectHandlerRegistry effectHandlers)
    {
        _defaultHandler = defaultHandler;
        _damageHandler = (effectHandlers.GetRequired(BattleCodes.Damage) as DamageEffectHandler) ?? new DamageEffectHandler();
    }

    public static QuocNhanGraduationSkillHandler Create(DefaultSkillHandler defaultHandler) =>
        new(defaultHandler, BattleEffectHandlerRegistry.CreateDefault());

    public bool CanHandle(BattleSkill skill) =>
        skill.Id.Equals(QuocNhanGraduationSkillCodes.Basic, StringComparison.OrdinalIgnoreCase) ||
        skill.Id.Equals(QuocNhanGraduationSkillCodes.Energy, StringComparison.OrdinalIgnoreCase);

    public SkillExecutionResult Execute(SkillExecutionContext context) =>
        context.Skill.Id.Equals(QuocNhanGraduationSkillCodes.Basic, StringComparison.OrdinalIgnoreCase)
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

        // 1. Deal 120% Magic Damage
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
            return result;
        }

        // 2. Luan Diem mechanism
        var luanDiemBonusPercent = primaryEffect.GetDecimal("LUAN_DIEM_BONUS_PER_STACK", 4m);
        var existingLuanDiem = target.StatusEffects.FirstOrDefault(s =>
            s.EffectTypeCode.Equals(BattleCodes.LuanDiem, StringComparison.OrdinalIgnoreCase) &&
            s.SourceHeroId == context.Actor.Id);

        if (existingLuanDiem == null)
        {
            var instanceId = $"{context.Actor.Id}:{context.Skill.Id}:{BattleCodes.LuanDiem}:{target.Id}";
            target.StatusEffects.Add(new BattleStatusEffect
            {
                InstanceId = instanceId,
                EffectTypeCode = BattleCodes.LuanDiem,
                SourceSkillId = context.Skill.Id,
                SourceHeroId = context.Actor.Id,
                RemainingTurns = 3,
                AppliedTurn = context.Turn,
                Stacks = 1,
                MaxStacks = 3,
                IncomingDamageBonusPerStackPercent = luanDiemBonusPercent,
                IncomingDamageBonusRestrictedToSource = true,
                Dispellable = true
            });

            result.Events.Add(new PendingBattleEvent
            {
                EventType = "STATUS_APPLIED",
                ActorId = context.Actor.Id,
                TargetId = target.Id,
                SkillId = context.Skill.Id,
                EffectTypeCode = BattleCodes.LuanDiem,
                RemainingTurns = 3,
                CurrentStacks = 1,
                MaxStacks = 3,
                TimelineOffsetMs = 900,
                PhaseCode = "IMPACT"
            });
        }
        else if (existingLuanDiem.Stacks < 3)
        {
            var prevStacks = existingLuanDiem.Stacks;
            existingLuanDiem.Stacks += 1;
            existingLuanDiem.RemainingTurns = 3;

            result.Events.Add(new PendingBattleEvent
            {
                EventType = BattleCodes.StatusStackChanged,
                ActorId = context.Actor.Id,
                TargetId = target.Id,
                SkillId = context.Skill.Id,
                EffectTypeCode = BattleCodes.LuanDiem,
                PreviousStacks = prevStacks,
                CurrentStacks = existingLuanDiem.Stacks,
                MaxStacks = 3,
                RemainingTurns = 3,
                TimelineOffsetMs = 900,
                PhaseCode = "IMPACT"
            });
        }
        else
        {
            // Already at 3 stacks: Do not add 4th stack. Reduce 10% Magic Resistance for 2 turns (refresh, don't stack)
            var debuffInstanceId = $"{context.Actor.Id}:{context.Skill.Id}:MR_DEBUFF:{target.Id}";
            var existingDebuff = target.StatusEffects.FirstOrDefault(s => s.InstanceId == debuffInstanceId);

            if (existingDebuff != null)
            {
                existingDebuff.RemainingTurns = 2;
                result.Events.Add(new PendingBattleEvent
                {
                    EventType = BattleCodes.StatusRefreshed,
                    ActorId = context.Actor.Id,
                    TargetId = target.Id,
                    SkillId = context.Skill.Id,
                    EffectTypeCode = BattleCodes.StatDebuff,
                    RemainingTurns = 2,
                    TimelineOffsetMs = 950,
                    PhaseCode = "IMPACT"
                });
            }
            else
            {
                target.StatusEffects.Add(new BattleStatusEffect
                {
                    InstanceId = debuffInstanceId,
                    EffectTypeCode = BattleCodes.StatDebuff,
                    SourceSkillId = context.Skill.Id,
                    SourceHeroId = context.Actor.Id,
                    RemainingTurns = 2,
                    AppliedTurn = context.Turn,
                    Stacks = 1,
                    MaxStacks = 1,
                    Value = -10m,
                    Dispellable = true,
                    ScaleModifiersWithStacks = false,
                    StatModifiers = [new BattleStatModifier("MAGIC_RESISTANCE", "PERCENT", -10m, "Kháng Phép")]
                });

                result.Events.Add(new PendingBattleEvent
                {
                    EventType = "STATUS_APPLIED",
                    ActorId = context.Actor.Id,
                    TargetId = target.Id,
                    SkillId = context.Skill.Id,
                    EffectTypeCode = BattleCodes.StatDebuff,
                    RemainingTurns = 2,
                    Value = -10,
                    StatModifiers = [new BattleStatModifier("MAGIC_RESISTANCE", "PERCENT", -10m, "Kháng Phép")],
                    TimelineOffsetMs = 950,
                    PhaseCode = "IMPACT"
                });
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

        // Targets: up to 3 living enemies
        var targets = _defaultHandler.ResolveTargets(primaryEffect.TargetTypeCode, context, primaryEffect).Take(3).ToList();
        var extraDamagePerStack = primaryEffect.GetDecimal("EXTRA_DAMAGE_PER_STACK_PERCENT", 0.18m);

        foreach (var target in targets)
        {
            result.TargetedCombatantIds.Add(target.Id);
            if (!target.IsAlive) continue;

            // 1. Initial 115% Magic Damage
            var dmgContext = new BattleEffectContext
            {
                Effect = primaryEffect,
                Skill = context.Skill,
                Actor = context.Actor,
                Target = target,
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
                result.DefeatedTargetIds.Add(target.Id);
            }

            // 2. Consume Luan Diem on this target
            var luanDiem = target.StatusEffects.FirstOrDefault(s =>
                s.EffectTypeCode.Equals(BattleCodes.LuanDiem, StringComparison.OrdinalIgnoreCase) &&
                s.SourceHeroId == context.Actor.Id);

            var stacksConsumed = luanDiem?.Stacks ?? 0;
            if (luanDiem != null)
            {
                target.StatusEffects.Remove(luanDiem);
                result.Events.Add(new PendingBattleEvent
                {
                    EventType = "STATUS_EXPIRED",
                    ActorId = context.Actor.Id,
                    TargetId = target.Id,
                    SkillId = context.Skill.Id,
                    EffectTypeCode = BattleCodes.LuanDiem,
                    PreviousStacks = stacksConsumed,
                    CurrentStacks = 0,
                    TimelineOffsetMs = 900,
                    PhaseCode = "IMPACT"
                });
            }

            // 3. Extra damage per consumed stack: 18% each
            if (stacksConsumed > 0 && target.IsAlive)
            {
                var extraDmgCoeff = stacksConsumed * extraDamagePerStack;
                var bonusEffect = new BattleSkillEffect
                {
                    EffectTypeCode = BattleCodes.Damage,
                    TargetTypeCode = BattleCodes.EnemySingle,
                    DamageSchoolCode = BattleCodes.Magic,
                    BaseValue = 0,
                    Scalings = [new BattleEffectScaling("MAGIC_DAMAGE", extraDmgCoeff, 0m)],
                    Parameters = new Dictionary<string, BattleSkillEffectParameter>
                    {
                        ["CAN_CRIT"] = new("CAN_CRIT", null, null, false, null)
                    }
                };

                var bonusContext = new BattleEffectContext
                {
                    Effect = bonusEffect,
                    Skill = context.Skill,
                    Actor = context.Actor,
                    Target = target,
                    SelectedTargets = [target],
                    Combatants = context.Combatants,
                    Random = context.Random,
                    Round = context.Round,
                    Turn = context.Turn,
                    ActionId = context.ActionId,
                    TimelineOffsetMs = 1000,
                    PhaseCode = "IMPACT"
                };

                var bonusResult = _damageHandler.ExecuteDamage(bonusContext);
                foreach (var evt in bonusResult.EmittedEvents)
                {
                    result.Events.Add(evt);
                }

                if (bonusResult.WasKilled && !result.DefeatedTargetIds.Contains(target.Id))
                {
                    result.DefeatedTargetIds.Add(target.Id);
                }
            }

            // 4. If consumed full 3 stacks: 50% chance to inflict SILENCE for 1 turn
            if (stacksConsumed >= 3 && target.IsAlive)
            {
                var roll = (decimal)context.Random.NextDouble() * 100m;
                var silenceChance = primaryEffect.GetDecimal("SILENCE_CHANCE_PERCENT", 50m);
                if (roll < silenceChance)
                {
                    var silenceInstanceId = $"{context.Actor.Id}:{context.Skill.Id}:{BattleCodes.Silence}:{target.Id}";
                    var existingSilence = target.StatusEffects.FirstOrDefault(s =>
                        s.EffectTypeCode.Equals(BattleCodes.Silence, StringComparison.OrdinalIgnoreCase));

                    if (existingSilence == null)
                    {
                        target.StatusEffects.Add(new BattleStatusEffect
                        {
                            InstanceId = silenceInstanceId,
                            EffectTypeCode = BattleCodes.Silence,
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
                        existingSilence.RemainingTurns = 1;
                    }

                    result.Events.Add(new PendingBattleEvent
                    {
                        EventType = "STATUS_APPLIED",
                        ActorId = context.Actor.Id,
                        TargetId = target.Id,
                        SkillId = context.Skill.Id,
                        EffectTypeCode = BattleCodes.Silence,
                        RemainingTurns = 1,
                        TimelineOffsetMs = 1100,
                        PhaseCode = "IMPACT"
                    });
                }
            }
        }

        return result;
    }
}
