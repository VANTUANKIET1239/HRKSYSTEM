using GAME.Domain.Battle.Reactions;

namespace GAME.Domain.Battle.Effects;

public sealed class DamageEffectExecutionResult
{
    public int ActualDamage { get; init; }
    public int ShieldAbsorbed { get; init; }
    public bool WasHit { get; init; }
    public bool WasCrit { get; init; }
    public bool WasKilled { get; init; }
    public IReadOnlyList<PendingBattleEvent> EmittedEvents { get; init; } = [];
}

public sealed class DamageEffectHandler : IBattleEffectHandler
{
    private readonly BattleStatusReactionHandlerRegistry _reactionHandlers;
    private readonly DamageRedirectHandlerRegistry _redirectHandlers;

    public DamageEffectHandler(
        BattleStatusReactionHandlerRegistry? reactionHandlers = null,
        DamageRedirectHandlerRegistry? redirectHandlers = null)
    {
        _reactionHandlers = reactionHandlers ?? BattleStatusReactionHandlerRegistry.CreateDefault();
        _redirectHandlers = redirectHandlers ?? DamageRedirectHandlerRegistry.CreateDefault();
    }

    public string EffectTypeCode => BattleCodes.Damage;

    public IReadOnlyList<PendingBattleEvent> Apply(BattleEffectContext context) =>
        ExecuteDamage(context).EmittedEvents;

    public DamageEffectExecutionResult ExecuteDamage(BattleEffectContext context)
    {
        var effect = context.Effect;
        var actor = context.Actor;
        var target = context.Target;
        var rawDamage = BattleStatCalculator.CalculateEffectValue(effect, actor);
        var school = effect.DamageSchoolCode ?? BattleCodes.Physical;

        var defense = school.Equals(BattleCodes.Magic, StringComparison.OrdinalIgnoreCase)
            ? BattleStatCalculator.GetEffectiveStat(target, "MAGIC_RESISTANCE")
            : BattleStatCalculator.GetEffectiveStat(target, "DEF");

        var armorIgnorePercent = effect.GetDecimal("ARMOR_IGNORE_PERCENT", 0m);
        if (armorIgnorePercent > 0m)
        {
            defense = Math.Max(0m, defense * (100m - armorIgnorePercent) / 100m);
        }

        var damage = school.Equals(BattleCodes.True, StringComparison.OrdinalIgnoreCase)
            ? Math.Max(1, (int)Math.Round(rawDamage))
            : BattleStatCalculator.MitigateDamage(rawDamage, defense, target.DefenseMitigationConstant);

        var canCrit = effect.GetBool("CAN_CRIT", true);
        var isCrit = false;
        if (canCrit && damage > 0)
        {
            var critBonus = effect.GetDecimal("CRIT_CHANCE_BONUS_PERCENT", 0m);
            var effectiveCritChance = actor.CritChance + critBonus;
            isCrit = (decimal)context.Random.NextDouble() * 100m < effectiveCritChance;
            if (isCrit) damage = Math.Max(1, (int)Math.Round(damage * actor.CritDamage / 100m));
        }

        // bonus dame calculation
        if (damage > 0)
        {
            var markPercent = SumStatusPercent(target, BattleCodes.Mark);
            var reductionPercent = Math.Clamp(SumStatusPercent(target, BattleCodes.DamageReduction), 0m, 90m);

            var attackerDamageBonusPercent = effect.GetDecimal("DAMAGE_BONUS_PERCENT", 0m);
            if (!effect.GetBool("IGNORE_OUTGOING_STATUS_DAMAGE_BONUS", false))
            {
                attackerDamageBonusPercent += actor.StatusEffects
                    .Where(status => status.IsActive)
                    .Sum(status => (status.OutgoingDamageBonusPerStackPercent != 0m
                        ? status.OutgoingDamageBonusPerStackPercent
                        : status.DamageBonusPerStackPercent) * status.Stacks);
            }

            var targetVulnerabilityPercent = target.StatusEffects
                .Where(status => status.IsActive &&
                    (!status.IncomingDamageBonusRestrictedToSource || status.SourceHeroId == actor.Id))
                .Sum(status => status.IncomingDamageBonusPerStackPercent * status.Stacks);

            damage = Math.Max(1, (int)Math.Round(damage * (100m + markPercent + targetVulnerabilityPercent) / 100m * (100m - reductionPercent) / 100m * (100m + attackerDamageBonusPercent) / 100m));
        }

        var events = new List<PendingBattleEvent>();

        // Check damage redirection (e.g. Guardian) before target shields & HP absorption
        var actionId = context.ActionId ?? $"turn_{context.Turn}_actor_{actor.Id}";
        var isRedirect = effect.GetBool("IS_REDIRECT", false);
        var ignoreGuardian = effect.GetBool("IGNORE_GUARDIAN_REDIRECT", false);

        if (!isRedirect && !ignoreGuardian && actor.Id != target.Id && damage > 0)
        {
            var redirectContext = new DamageRedirectContext
            {
                Actor = actor,
                Target = target,
                Skill = context.Skill,
                Effect = effect,
                IncomingDamage = damage,
                DamageSchoolCode = school,
                ActionId = actionId,
                Round = context.Round,
                Turn = context.Turn,
                Combatants = context.Combatants,
                Random = context.Random,
                TimelineOffsetMs = context.TimelineOffsetMs,
                PhaseCode = context.PhaseCode,
                IsRedirect = false
            };

            foreach (var handler in _redirectHandlers.Handlers)
            {
                var redirectResult = handler.HandleRedirect(redirectContext);
                if (redirectResult != null)
                {
                    damage = redirectResult.TargetDamage;
                    events.AddRange(redirectResult.EmittedEvents);
                    break;
                }
            }
        }

        // Consume one-hit / consume-on-hit buffs on target
        var consumeOnHitStatuses = target.StatusEffects
            .Where(x => x.RemainingTurns > 0 && x.ConsumeOnHit)
            .ToList();
        foreach (var cons in consumeOnHitStatuses)
        {
            target.StatusEffects.Remove(cons);
            events.Add(new PendingBattleEvent
            {
                EventType = "STATUS_EXPIRED",
                ActorId = cons.SourceHeroId,
                TargetId = target.Id,
                SkillId = cons.SourceSkillId,
                EffectTypeCode = cons.EffectTypeCode
            });
        }

        // Shields absorption
        var remainingDamage = damage;
        var totalShieldAbsorbed = 0;
        foreach (var shield in target.StatusEffects.Where(x =>
                     x.EffectTypeCode.Equals(BattleCodes.Shield, StringComparison.OrdinalIgnoreCase) && x.ShieldRemaining > 0).ToList())
        {
            var absorbed = Math.Min(remainingDamage, shield.ShieldRemaining);
            shield.ShieldRemaining -= absorbed;
            remainingDamage -= absorbed;
            totalShieldAbsorbed += absorbed;
            events.Add(new PendingBattleEvent
            {
                EventType = "SHIELD_ABSORBED",
                ActorId = actor.Id,
                TargetId = target.Id,
                SkillId = context.Skill.Id,
                EffectTypeCode = BattleCodes.Shield,
                Value = absorbed,
                StatusInstanceId = shield.InstanceId
            });
            if (shield.ShieldRemaining == 0)
            {
                target.StatusEffects.Remove(shield);
                events.Add(new PendingBattleEvent
                {
                    EventType = "STATUS_EXPIRED",
                    ActorId = shield.SourceHeroId,
                    TargetId = target.Id,
                    SkillId = shield.SourceSkillId,
                    EffectTypeCode = BattleCodes.Shield,
                    StatusInstanceId = shield.InstanceId
                });
            }
            if (remainingDamage == 0) break;
        }

        var canKill = effect.GetBool("CAN_KILL", true);
        var hpBefore = target.Hp;
        var hpAfter = canKill ? Math.Max(0, hpBefore - remainingDamage) : Math.Max(1, hpBefore - remainingDamage);
        var actualHpDamage = hpBefore - hpAfter;
        target.Hp = hpAfter;

        events.Add(new PendingBattleEvent
        {
            EventType = "DAMAGE",
            ActorId = actor.Id,
            TargetId = target.Id,
            SkillId = context.Skill.Id,
            EffectTypeCode = effect.EffectTypeCode,
            DamageSchoolCode = school,
            // Report HP actually removed, not pre-overkill damage.
            Value = actualHpDamage,
            HpBefore = hpBefore,
            HpAfter = target.Hp,
            IsCrit = isCrit
        });

        var killed = hpBefore > 0 && target.Hp == 0;
        if (killed)
        {
            events.Add(new PendingBattleEvent
            {
                EventType = "DEATH",
                ActorId = actor.Id,
                TargetId = target.Id,
                SkillId = context.Skill.Id
            });
        }

        if (remainingDamage > 0 && actor.Id != target.Id && actor.IsAlive)
        {
            var reflectionPercent = SumStatusPercent(target, BattleCodes.DamageReflection);
            if (reflectionPercent > 0)
            {
                var reflected = Math.Max(1, (int)Math.Round(remainingDamage * reflectionPercent / 100m));
                var actorHpBefore = actor.Hp;
                actor.Hp = Math.Max(0, actor.Hp - reflected);
                var actualReflectedDamage = actorHpBefore - actor.Hp;
                events.Add(new PendingBattleEvent
                {
                    EventType = "DAMAGE",
                    ActorId = target.Id,
                    TargetId = actor.Id,
                    SkillId = context.Skill.Id,
                    EffectTypeCode = BattleCodes.DamageReflection,
                    DamageSchoolCode = BattleCodes.True,
                    Value = actualReflectedDamage,
                    HpBefore = actorHpBefore,
                    HpAfter = actor.Hp
                });
                if (actorHpBefore > 0 && actor.Hp == 0)
                {
                    events.Add(new PendingBattleEvent
                    {
                        EventType = "DEATH",
                        ActorId = target.Id,
                        TargetId = actor.Id,
                        SkillId = context.Skill.Id,
                        EffectTypeCode = BattleCodes.DamageReflection
                    });
                }
            }
        }

        var totalActualDamage = totalShieldAbsorbed + actualHpDamage;
        var wasHit = rawDamage > 0 && (damage > 0 || totalShieldAbsorbed > 0);

        if (target.IsAlive && actualHpDamage > 0 && wasHit)
        {
            var damagedContext = new BattleDamagedContext
            {
                Actor = actor,
                Target = target,
                Skill = context.Skill,
                Effect = effect,
                ActualHpDamage = actualHpDamage,
                ShieldAbsorbed = totalShieldAbsorbed,
                WasHit = wasHit,
                ActionId = actionId,
                Round = context.Round,
                Turn = context.Turn,
                Combatants = context.Combatants,
                Random = context.Random
            };

            foreach (var status in target.StatusEffects.ToList())
            {
                var reactionHandler = _reactionHandlers.GetHandler(status.EffectTypeCode);
                if (reactionHandler != null)
                {
                    var reactionEvents = reactionHandler.OnDamaged(status, damagedContext);
                    events.AddRange(reactionEvents);
                }
            }
        }

        return new DamageEffectExecutionResult
        {
            ActualDamage = totalActualDamage,
            ShieldAbsorbed = totalShieldAbsorbed,
            WasHit = wasHit,
            WasCrit = isCrit,
            WasKilled = killed,
            EmittedEvents = events
        };
    }

    private static decimal SumStatusPercent(BattleCombatant target, string effectCode) =>
        target.StatusEffects.Where(x => x.RemainingTurns > 0 &&
                x.EffectTypeCode.Equals(effectCode, StringComparison.OrdinalIgnoreCase))
            .Sum(x => x.Value * x.Stacks);
}
