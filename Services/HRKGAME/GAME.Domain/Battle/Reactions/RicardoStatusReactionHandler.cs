using GAME.Domain.Battle.Effects;

namespace GAME.Domain.Battle.Reactions;

public sealed class RicardoStatusReactionHandler : IBattleStatusReactionHandler
{
    public string EffectTypeCode => BattleCodes.Ricardo;

    public IReadOnlyList<PendingBattleEvent> OnDamaged(BattleStatusEffect status, BattleDamagedContext context)
    {
        // 1. Must be a direct damage effect
        if (!context.Effect.EffectTypeCode.Equals(BattleCodes.Damage, StringComparison.OrdinalIgnoreCase))
            return [];

        // 2. Attacker must be an enemy (not ally, not self)
        if (context.Actor == null || context.Actor.Team == context.Target.Team || context.Actor.Id == context.Target.Id)
            return [];

        // 3. Must have actually suffered HP reduction (> 0)
        if (context.ActualHpDamage <= 0 || !context.WasHit)
            return [];

        // 4. Multi-hit action guard: max 1 stack per enemy action
        if (!string.IsNullOrEmpty(context.ActionId) &&
            string.Equals(status.LastProcessedActionId, context.ActionId, StringComparison.Ordinal))
        {
            return [];
        }

        status.LastProcessedActionId = context.ActionId;

        // 5. Stacks cannot exceed MaxStacks
        if (status.Stacks >= status.MaxStacks)
        {
            return [];
        }

        var previousStacks = status.Stacks;
        status.Stacks = Math.Min(status.MaxStacks, status.Stacks + 1);

        var events = new List<PendingBattleEvent>
        {
            new()
            {
                EventType = BattleCodes.StatusStackChanged,
                ActorId = context.Actor.Id,
                TargetId = context.Target.Id,
                SkillId = status.SourceSkillId,
                EffectTypeCode = BattleCodes.Ricardo,
                Value = status.Stacks,
                PreviousStacks = previousStacks,
                CurrentStacks = status.Stacks,
                MaxStacks = status.MaxStacks,
                StatModifiers = status.StatModifiers,
                TimelineOffsetMs = context.TimelineOffsetMs,
                PhaseCode = context.PhaseCode ?? "IMPACT"
            }
        };

        if (status.Stacks >= status.MaxStacks && previousStacks < status.MaxStacks)
        {
            events.Add(new PendingBattleEvent
            {
                EventType = BattleCodes.RicardoRageReady,
                ActorId = context.Actor.Id,
                TargetId = context.Target.Id,
                SkillId = status.SourceSkillId,
                EffectTypeCode = BattleCodes.Ricardo,
                Value = status.Stacks,
                PreviousStacks = previousStacks,
                CurrentStacks = status.Stacks,
                MaxStacks = status.MaxStacks,
                TimelineOffsetMs = context.TimelineOffsetMs,
                PhaseCode = context.PhaseCode ?? "IMPACT"
            });
        }

        return events;
    }
}
