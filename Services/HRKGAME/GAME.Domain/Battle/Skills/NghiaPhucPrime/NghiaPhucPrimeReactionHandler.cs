using GAME.Domain.Battle.Effects;
using GAME.Domain.Battle.Reactions;

namespace GAME.Domain.Battle.Skills.NghiaPhucPrime;

public sealed class NghiaPhucPrimeReactionHandler : IBattleEffectHandler, IDamageRedirectHandler, ITurnEndEffectHandler
{
    public string EffectTypeCode => NghiaPhucPrimeSkillCodes.Guardian;

    public IReadOnlyList<PendingBattleEvent> Apply(BattleEffectContext context) => [];

    public DamageRedirectResult? HandleRedirect(DamageRedirectContext context)
    {
        // 1. Direct damage effect only
        if (!context.Effect.EffectTypeCode.Equals(BattleCodes.Damage, StringComparison.OrdinalIgnoreCase))
            return null;

        // 2. Cannot redirect DOT, reflection, self-damage, or already redirected damage
        if (context.IsRedirect ||
            context.Actor.Id == context.Target.Id ||
            context.Effect.GetBool("IGNORE_GUARDIAN_REDIRECT", false) ||
            context.Effect.EffectTypeCode.Equals(BattleCodes.Bleed, StringComparison.OrdinalIgnoreCase) ||
            context.Effect.EffectTypeCode.Equals(BattleCodes.BleedDamage, StringComparison.OrdinalIgnoreCase) ||
            context.Effect.EffectTypeCode.Equals(BattleCodes.DamageReflection, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        // 3. Find active Guardian on the same team who is alive, has > 1 HP, and is NOT the target
        var guardian = context.Combatants.FirstOrDefault(c =>
            c.Team == context.Target.Team &&
            c.Id != context.Target.Id &&
            c.IsAlive &&
            c.Hp > 1 &&
            c.StatusEffects.Any(s => s.EffectTypeCode.Equals(NghiaPhucPrimeSkillCodes.Guardian, StringComparison.OrdinalIgnoreCase) && s.RemainingTurns > 0));

        if (guardian == null) return null;

        var guardianStatus = guardian.StatusEffects.First(s =>
            s.EffectTypeCode.Equals(NghiaPhucPrimeSkillCodes.Guardian, StringComparison.OrdinalIgnoreCase) && s.RemainingTurns > 0);

        // 4. Calculate Redirection
        var redirectPercent = 35m;
        var requested = Math.Max(1, (int)Math.Round(context.IncomingDamage * redirectPercent / 100m));

        // Prime cannot drop below GUARDIAN_MIN_HP (1 HP)
        var primeShields = guardian.StatusEffects
            .Where(s => s.EffectTypeCode.Equals(BattleCodes.Shield, StringComparison.OrdinalIgnoreCase))
            .Sum(s => s.ShieldRemaining);
        var maxHpLoss = Math.Max(0, guardian.Hp - 1);
        var maxAbsorb = primeShields + maxHpLoss;

        if (maxAbsorb <= 0)
        {
            // Prime is at 1 HP with 0 shields; cannot take redirected damage. All stays with target.
            return null;
        }

        var redirectActual = Math.Min(requested, maxAbsorb);
        var allyDamageAfterRedirect = context.IncomingDamage - redirectActual;

        var events = new List<PendingBattleEvent>();

        // 5. Apply redirect damage to Guardian (shields first, then HP)
        var remainingRedirect = redirectActual;
        var shieldAbsorbed = 0;

        foreach (var shield in guardian.StatusEffects.Where(s =>
                     s.EffectTypeCode.Equals(BattleCodes.Shield, StringComparison.OrdinalIgnoreCase) && s.ShieldRemaining > 0).ToList())
        {
            var absorbed = Math.Min(remainingRedirect, shield.ShieldRemaining);
            shield.ShieldRemaining -= absorbed;
            remainingRedirect -= absorbed;
            shieldAbsorbed += absorbed;

            events.Add(new PendingBattleEvent
            {
                EventType = "SHIELD_ABSORBED",
                ActorId = context.Actor.Id,
                TargetId = guardian.Id,
                SkillId = context.Skill.Id,
                EffectTypeCode = BattleCodes.Shield,
                Value = absorbed,
                StatusInstanceId = shield.InstanceId,
                TimelineOffsetMs = context.TimelineOffsetMs,
                PhaseCode = context.PhaseCode ?? "IMPACT"
            });

            if (shield.ShieldRemaining == 0)
            {
                guardian.StatusEffects.Remove(shield);
                events.Add(new PendingBattleEvent
                {
                    EventType = "STATUS_EXPIRED",
                    ActorId = shield.SourceHeroId,
                    TargetId = guardian.Id,
                    SkillId = shield.SourceSkillId,
                    EffectTypeCode = BattleCodes.Shield,
                    StatusInstanceId = shield.InstanceId,
                    TimelineOffsetMs = context.TimelineOffsetMs,
                    PhaseCode = context.PhaseCode ?? "IMPACT"
                });
            }

            if (remainingRedirect == 0) break;
        }

        var guardianHpBefore = guardian.Hp;
        var guardianHpAfter = Math.Max(1, guardianHpBefore - remainingRedirect);
        guardian.Hp = guardianHpAfter;

        // 6. Emit PRIME_GUARD_REDIRECTED event
        events.Add(new PendingBattleEvent
        {
            EventType = NghiaPhucPrimeSkillCodes.EventGuardRedirected,
            ActorId = context.Actor.Id,
            TargetId = context.Target.Id,
            SourceHeroId = guardian.Id,
            Value = redirectActual,
            HpBefore = guardianHpBefore,
            HpAfter = guardianHpAfter,
            OriginalDamage = context.IncomingDamage,
            RedirectRequested = requested,
            RedirectActual = redirectActual,
            AllyDamageAfterRedirect = allyDamageAfterRedirect,
            GuardianHpBefore = guardianHpBefore,
            GuardianHpAfter = guardianHpAfter,
            ActionId = context.ActionId,
            TimelineOffsetMs = context.TimelineOffsetMs,
            PhaseCode = context.PhaseCode ?? "IMPACT"
        });

        // Emit direct DAMAGE event on Guardian for UI combat text popup
        if (redirectActual > 0)
        {
            events.Add(new PendingBattleEvent
            {
                EventType = BattleCodes.Damage,
                ActorId = context.Actor.Id,
                TargetId = guardian.Id,
                SkillId = context.Skill.Id,
                EffectTypeCode = NghiaPhucPrimeSkillCodes.Guardian,
                DamageSchoolCode = context.DamageSchoolCode,
                Value = guardianHpBefore - guardianHpAfter,
                HpBefore = guardianHpBefore,
                HpAfter = guardianHpAfter,
                ActionId = context.ActionId,
                TimelineOffsetMs = context.TimelineOffsetMs,
                PhaseCode = context.PhaseCode ?? "IMPACT"
            });
        }

        // 7. Accumulate Áp Lực (PRIME_PRESSURE) - max 1 stack per enemy action
        var actionKey = $"PRESSURE_{context.ActionId}";
        if (redirectActual > 0 && !guardian.ProcessedActionIds.Contains(actionKey))
        {
            guardian.ProcessedActionIds.Add(actionKey);

            var pressureStatus = guardian.StatusEffects.FirstOrDefault(s =>
                s.EffectTypeCode.Equals(NghiaPhucPrimeSkillCodes.Pressure, StringComparison.OrdinalIgnoreCase));

            var previousStacks = pressureStatus?.Stacks ?? 0;
            if (pressureStatus == null)
            {
                pressureStatus = new BattleStatusEffect
                {
                    InstanceId = $"{guardian.Id}:{NghiaPhucPrimeSkillCodes.Pressure}",
                    EffectTypeCode = NghiaPhucPrimeSkillCodes.Pressure,
                    SourceHeroId = guardian.Id,
                    SourceSkillId = NghiaPhucPrimeSkillCodes.Ultimate,
                    RemainingTurns = -1, // Permanent until released
                    AppliedTurn = context.Turn,
                    Stacks = 1,
                    MaxStacks = 5,
                    Value = 0
                };
                guardian.StatusEffects.Add(pressureStatus);
            }
            else
            {
                pressureStatus.Stacks = Math.Min(5, pressureStatus.Stacks + 1);
            }

            events.Add(new PendingBattleEvent
            {
                EventType = NghiaPhucPrimeSkillCodes.EventPressureChanged,
                ActorId = context.Actor.Id,
                TargetId = guardian.Id,
                SkillId = context.Skill.Id,
                EffectTypeCode = NghiaPhucPrimeSkillCodes.Pressure,
                Value = pressureStatus.Stacks,
                PreviousStacks = previousStacks,
                CurrentStacks = pressureStatus.Stacks,
                MaxStacks = 5,
                ActionId = context.ActionId,
                TimelineOffsetMs = context.TimelineOffsetMs + 40,
                PhaseCode = context.PhaseCode ?? "IMPACT"
            });

            // 8. If 5 stacks reached: immediate pressure release!
            if (pressureStatus.Stacks >= 5)
            {
                var releaseEvents = ExecutePressureRelease(guardian, pressureStatus.Stacks, context.Combatants, context.Turn, context.Round, context.Random, context.ActionId, context.TimelineOffsetMs + 100);
                events.AddRange(releaseEvents);

                // Remove Guardian status and Pressure status early
                guardian.StatusEffects.Remove(guardianStatus);
                guardian.StatusEffects.Remove(pressureStatus);
            }
        }

        return new DamageRedirectResult
        {
            TargetDamage = allyDamageAfterRedirect,
            RedirectedDamage = redirectActual,
            EmittedEvents = events
        };
    }

    public IReadOnlyList<PendingBattleEvent> OnTurnEnd(
        BattleStatusEffect status,
        BattleCombatant actor,
        int round,
        int turn,
        IReadOnlyList<BattleCombatant> combatants)
    {
        // When PRIME_GUARDIAN turns expire (RemainingTurns == 0) and Prime has >= 1 stack of Pressure:
        // release according to current stacks
        if (!status.EffectTypeCode.Equals(NghiaPhucPrimeSkillCodes.Guardian, StringComparison.OrdinalIgnoreCase))
            return [];

        if (status.RemainingTurns > 0) return [];

        var pressureStatus = actor.StatusEffects.FirstOrDefault(s =>
            s.EffectTypeCode.Equals(NghiaPhucPrimeSkillCodes.Pressure, StringComparison.OrdinalIgnoreCase));

        if (pressureStatus != null && pressureStatus.Stacks >= 1)
        {
            var stacks = pressureStatus.Stacks;
            actor.StatusEffects.Remove(pressureStatus);
            var random = new Random();
            return ExecutePressureRelease(actor, stacks, combatants, turn, round, random, $"turn_{turn}_guardian_expire", 0);
        }

        return [];
    }

    public static IReadOnlyList<PendingBattleEvent> ExecutePressureRelease(
        BattleCombatant prime,
        int stacks,
        IReadOnlyList<BattleCombatant> combatants,
        int turn,
        int round,
        Random random,
        string actionId,
        int timelineOffsetMs)
    {
        if (stacks <= 0) return [];

        var events = new List<PendingBattleEvent>();

        // 1. Heal Prime: Max HP * 2% * stacks
        var healPercent = 2m * stacks;
        var rawHeal = (int)Math.Round(prime.MaxHp * healPercent / 100m);
        var hpBeforeHeal = prime.Hp;
        prime.Hp = Math.Min(prime.MaxHp, prime.Hp + rawHeal);
        var actualHeal = prime.Hp - hpBeforeHeal;

        events.Add(new PendingBattleEvent
        {
            EventType = BattleCodes.Heal,
            ActorId = prime.Id,
            TargetId = prime.Id,
            SkillId = NghiaPhucPrimeSkillCodes.Ultimate,
            EffectTypeCode = BattleCodes.Heal,
            Value = actualHeal,
            HpBefore = hpBeforeHeal,
            HpAfter = prime.Hp,
            ActionId = actionId,
            TimelineOffsetMs = timelineOffsetMs,
            PhaseCode = "IMPACT"
        });

        // 2. Physical Damage to all alive enemies: Effective DEF * 20% * stacks
        var primeDef = BattleStatCalculator.GetEffectiveStat(prime, "DEF");
        var rawDamagePerEnemy = (primeDef * 20m * stacks) / 100m;

        var enemies = combatants.Where(c => c.Team != prime.Team && c.IsAlive).ToList();
        foreach (var enemy in enemies)
        {
            var enemyDef = BattleStatCalculator.GetEffectiveStat(enemy, "DEF");
            var enemyDamage = Math.Max(1, (int)Math.Round(rawDamagePerEnemy * 100m / (100m + Math.Max(0m, enemyDef))));

            var enemyHpBefore = enemy.Hp;
            enemy.Hp = Math.Max(0, enemy.Hp - enemyDamage);
            var actualHpDamage = enemyHpBefore - enemy.Hp;

            events.Add(new PendingBattleEvent
            {
                EventType = BattleCodes.Damage,
                ActorId = prime.Id,
                TargetId = enemy.Id,
                SkillId = NghiaPhucPrimeSkillCodes.Ultimate,
                EffectTypeCode = BattleCodes.Damage,
                DamageSchoolCode = BattleCodes.Physical,
                Value = actualHpDamage,
                HpBefore = enemyHpBefore,
                HpAfter = enemy.Hp,
                IsCrit = false,
                ActionId = actionId,
                TimelineOffsetMs = timelineOffsetMs + 40,
                PhaseCode = "IMPACT"
            });

            if (enemyHpBefore > 0 && enemy.Hp == 0)
            {
                events.Add(new PendingBattleEvent
                {
                    EventType = "DEATH",
                    ActorId = prime.Id,
                    TargetId = enemy.Id,
                    SkillId = NghiaPhucPrimeSkillCodes.Ultimate,
                    ActionId = actionId,
                    TimelineOffsetMs = timelineOffsetMs + 60,
                    PhaseCode = "IMPACT"
                });
            }
        }

        // 3. Emit PRIME_PRESSURE_RELEASED event
        events.Add(new PendingBattleEvent
        {
            EventType = NghiaPhucPrimeSkillCodes.EventPressureReleased,
            ActorId = prime.Id,
            TargetId = prime.Id,
            SkillId = NghiaPhucPrimeSkillCodes.Ultimate,
            EffectTypeCode = NghiaPhucPrimeSkillCodes.Pressure,
            Value = stacks,
            ActionId = actionId,
            TimelineOffsetMs = timelineOffsetMs,
            PhaseCode = "IMPACT"
        });

        return events;
    }
}
