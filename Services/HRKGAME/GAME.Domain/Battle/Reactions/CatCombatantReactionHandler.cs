using GAME.Domain.Battle.Effects;

namespace GAME.Domain.Battle.Reactions;

public sealed class CatCombatantReactionHandler : IBattleCombatantReactionHandler
{
    public IReadOnlyList<PendingBattleEvent> OnTargeted(BattleCombatant target, BattleTargetedReactionContext context) => [];

    public IReadOnlyList<PendingBattleEvent> OnCombatantDefeated(BattleCombatant observer, BattleCombatantDefeatedReactionContext context) => [];

    public IReadOnlyList<PendingBattleEvent> OnActionCompleted(BattleCombatant actor, BattleActionCompletedReactionContext context)
    {
        var events = new List<PendingBattleEvent>();

        // Part 1: Lifesteal / Healing from Cat Scratch & Deep Cat Scratch
        if (CatScratchTracker.TryConsumeDamage(context.ActionId, actor.Id, out var normalDmg, out var deepDmg))
        {
            var healAmount = (int)Math.Round(normalDmg * 0.05m + deepDmg * 0.10m);
            if (healAmount > 0 && actor.IsAlive && actor.Hp < actor.MaxHp)
            {
                var actualHeal = Math.Min(healAmount, actor.MaxHp - actor.Hp);
                var hpBefore = actor.Hp;
                actor.Hp += actualHeal;

                events.Add(new PendingBattleEvent
                {
                    EventType = "HEAL",
                    ActorId = actor.Id,
                    TargetId = actor.Id,
                    SkillId = context.Skill.Id,
                    EffectTypeCode = BattleCodes.Heal,
                    Value = actualHeal,
                    HpBefore = hpBefore,
                    HpAfter = actor.Hp,
                    TimelineOffsetMs = context.TimelineOffsetMs,
                    PhaseCode = "RECOVERY"
                });
            }
        }

        // Part 2: Cat Companion Assist & Deep Cat Scratch application
        // Host must have active Cat Companion
        var hasCompanion = actor.StatusEffects.Any(s =>
            s.RemainingTurns > 0 && s.EffectTypeCode.Equals(BattleCodes.CatCompanion, StringComparison.OrdinalIgnoreCase));

        if (hasCompanion)
        {
            // Must have dealt direct damage in this action
            var directDamagedEnemyIds = context.ExecutionResult.Events
                .Where(e => e.EventType == "DAMAGE" &&
                            e.ActorId == actor.Id &&
                            e.TargetId.HasValue &&
                            e.Value > 0)
                .Select(e => e.TargetId!.Value)
                .Distinct()
                .ToList();

            foreach (var targetId in directDamagedEnemyIds)
            {
                var target = context.Combatants.FirstOrDefault(x => x.Id == targetId && x.IsAlive);
                if (target == null) continue;

                // 1. Remove normal Cat Scratch if present (Deep Cat Scratch replaces it)
                var existingNormal = target.StatusEffects.FirstOrDefault(s =>
                    s.EffectTypeCode.Equals(BattleCodes.CatScratch, StringComparison.OrdinalIgnoreCase));
                if (existingNormal != null)
                {
                    target.StatusEffects.Remove(existingNormal);
                    events.Add(new PendingBattleEvent
                    {
                        EventType = "STATUS_EXPIRED",
                        ActorId = actor.Id,
                        TargetId = target.Id,
                        SkillId = existingNormal.SourceSkillId,
                        EffectTypeCode = BattleCodes.CatScratch,
                        StatusInstanceId = existingNormal.InstanceId,
                        TimelineOffsetMs = context.TimelineOffsetMs,
                        PhaseCode = "RECOVERY"
                    });
                }

                // 2. Refresh or Apply Deep Cat Scratch
                var existingDeep = target.StatusEffects.FirstOrDefault(s =>
                    s.EffectTypeCode.Equals(BattleCodes.DeepCatScratch, StringComparison.OrdinalIgnoreCase));

                if (existingDeep != null)
                {
                    existingDeep.RemainingTurns = 2;
                    events.Add(new PendingBattleEvent
                    {
                        EventType = BattleCodes.StatusRefreshed,
                        ActorId = actor.Id,
                        TargetId = target.Id,
                        SkillId = context.Skill.Id,
                        EffectTypeCode = BattleCodes.DeepCatScratch,
                        RemainingTurns = 2,
                        StatusInstanceId = existingDeep.InstanceId,
                        TimelineOffsetMs = context.TimelineOffsetMs,
                        PhaseCode = "RECOVERY"
                    });
                }
                else
                {
                    var instanceId = $"{actor.Id}:{context.Skill.Id}:{BattleCodes.DeepCatScratch}:{target.Id}";
                    var newDeep = new BattleStatusEffect
                    {
                        InstanceId = instanceId,
                        EffectTypeCode = BattleCodes.DeepCatScratch,
                        SourceSkillId = context.Skill.Id,
                        SourceHeroId = actor.Id,
                        RemainingTurns = 2,
                        AppliedTurn = context.Turn,
                        Stacks = 1,
                        MaxStacks = 1,
                        Value = 20m,
                        IncomingDamageBonusPerStackPercent = 20m,
                        IncomingDamageBonusRestrictedToSource = false,
                        Dispellable = true
                    };
                    target.StatusEffects.Add(newDeep);

                    events.Add(new PendingBattleEvent
                    {
                        EventType = "STATUS_APPLIED",
                        ActorId = actor.Id,
                        TargetId = target.Id,
                        SkillId = context.Skill.Id,
                        EffectTypeCode = BattleCodes.DeepCatScratch,
                        RemainingTurns = 2,
                        Value = 20,
                        StatusInstanceId = instanceId,
                        TimelineOffsetMs = context.TimelineOffsetMs,
                        PhaseCode = "RECOVERY"
                    });
                }
            }
        }

        return events;
    }
}
