namespace GAME.Domain.Battle.Effects;

public sealed class ShieldEffectHandler : IBattleEffectHandler
{
    public string EffectTypeCode => BattleCodes.Shield;

    public IReadOnlyList<PendingBattleEvent> Apply(BattleEffectContext context)
    {
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
            Value = shield, RemainingTurns = duration
        }];
    }
}
