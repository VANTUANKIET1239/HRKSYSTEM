using GAME.Domain.Battle.Effects;

namespace GAME.Domain.Battle.Reactions;

public sealed class TinChiDanhDuReactionHandler : IBattleStatusReactionHandler
{
    public string EffectTypeCode => BattleCodes.TinChiDanhDu;

    public IReadOnlyList<PendingBattleEvent> OnDamaged(BattleStatusEffect status, BattleDamagedContext context)
    {
        // 1. Must be a direct damage effect (not DoT, not reflection)
        if (!context.Effect.EffectTypeCode.Equals(BattleCodes.Damage, StringComparison.OrdinalIgnoreCase))
            return [];
        if (context.Effect.GetBool("IS_DOT", false) || context.Effect.GetBool("IS_REFLECTION", false))
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

        // 5. Stacks cannot exceed MaxStacks (3)
        var maxStacks = status.MaxStacks > 0 ? status.MaxStacks : 3;
        if (status.Stacks >= maxStacks)
        {
            return [];
        }

        var previousStacks = status.Stacks;
        status.Stacks = Math.Min(maxStacks, status.Stacks + 1);
        context.Target.SetResource(BattleCodes.TinChiDanhDu, status.Stacks);

        return
        [
            new PendingBattleEvent
            {
                EventType = BattleCodes.StatusStackChanged,
                ActorId = context.Actor.Id,
                TargetId = context.Target.Id,
                SkillId = status.SourceSkillId,
                EffectTypeCode = BattleCodes.TinChiDanhDu,
                Value = status.Stacks,
                PreviousStacks = previousStacks,
                CurrentStacks = status.Stacks,
                MaxStacks = maxStacks,
                StatModifiers = status.StatModifiers,
                ResourceCode = BattleCodes.TinChiDanhDu,
                PreviousValue = previousStacks,
                CurrentValue = status.Stacks,
                TimelineOffsetMs = context.TimelineOffsetMs,
                PhaseCode = context.PhaseCode ?? "IMPACT"
            }
        ];
    }
}
