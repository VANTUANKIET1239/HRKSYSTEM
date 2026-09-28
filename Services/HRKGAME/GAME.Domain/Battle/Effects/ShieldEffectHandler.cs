namespace GAME.Domain.Battle.Effects;

public sealed class ShieldEffectHandler : IBattleEffectHandler
{
    public string EffectTypeCode => BattleCodes.Shield;

    public IReadOnlyList<PendingBattleEvent> Apply(BattleEffectContext context)
    {
        // If target has PANIC or SHIELD_BLOCK, new shields cannot be applied
        if (context.Target.StatusEffects.Any(x => x.RemainingTurns > 0 &&
            (x.EffectTypeCode.Equals(BattleCodes.Panic, StringComparison.OrdinalIgnoreCase) ||
             x.EffectTypeCode.Equals(BattleCodes.ShieldBlock, StringComparison.OrdinalIgnoreCase))))
        {
            return [];
        }

        var shield = Math.Max(1, (int)Math.Round(BattleStatCalculator.CalculateEffectValue(context.Effect, context.Actor)));
        var duration = Math.Max(1, context.Effect.DurationTurns);
        var instanceId = $"{context.Actor.Id}:{context.Skill.Id}:{EffectTypeCode}:{context.Target.Id}";
        var existing = context.Target.StatusEffects.FirstOrDefault(x => x.InstanceId == instanceId);
        if (existing == null)
        {
            context.Target.StatusEffects.Add(new BattleStatusEffect
            {
                InstanceId = instanceId, EffectTypeCode = EffectTypeCode,
                SourceSkillId = context.Skill.Id, SourceHeroId = context.Actor.Id,
                RemainingTurns = duration, AppliedTurn = context.Turn,
                ShieldRemaining = shield, Value = shield,
                StatModifiers = []
            });
        }
        else
        {
            existing.RemainingTurns = duration;
            existing.AppliedTurn = context.Turn;
            existing.ShieldRemaining = shield;
            existing.Value = shield;
        }

        return [new PendingBattleEvent
        {
            EventType = "SHIELD_APPLIED", ActorId = context.Actor.Id, TargetId = context.Target.Id,
            SkillId = context.Skill.Id, EffectTypeCode = EffectTypeCode,
            Value = shield, RemainingTurns = duration, StatusInstanceId = instanceId
        }];
    }
}
