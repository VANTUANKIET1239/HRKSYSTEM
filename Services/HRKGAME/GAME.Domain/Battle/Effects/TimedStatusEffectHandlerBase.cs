namespace GAME.Domain.Battle.Effects;

public abstract class TimedStatusEffectHandlerBase : IBattleEffectHandler
{
    public abstract string EffectTypeCode { get; }
    protected virtual decimal DefaultValue => 0m;

    public virtual IReadOnlyList<PendingBattleEvent> Apply(BattleEffectContext context)
    {
        var duration = Math.Max(1, context.Effect.DurationTurns);
        var calculated = BattleStatCalculator.CalculateEffectValue(context.Effect, context.Actor);
        var value = calculated == 0 ? DefaultValue : calculated;
        var instanceId = $"{context.Actor.Id}:{context.Skill.Id}:{EffectTypeCode}:{context.Target.Id}";
        var existing = context.Target.StatusEffects.FirstOrDefault(x => x.InstanceId == instanceId);

        if (existing == null)
        {
            context.Target.StatusEffects.Add(new BattleStatusEffect
            {
                InstanceId = instanceId,
                EffectTypeCode = EffectTypeCode,
                SourceSkillId = context.Skill.Id,
                SourceHeroId = context.Actor.Id,
                RemainingTurns = duration,
                AppliedTurn = context.Turn,
                Stacks = 1,
                MaxStacks = Math.Max(1, context.Effect.MaxStacks),
                Value = value,
                StatModifiers = context.Effect.StatModifiers
            });
        }
        else
        {
            existing.RemainingTurns = duration;
            existing.AppliedTurn = context.Turn;
            existing.Stacks = Math.Min(existing.MaxStacks, existing.Stacks + 1);
            existing.Value = value;
        }

        return [StatusApplied(context, duration, (int)Math.Round(value))];
    }

    protected static PendingBattleEvent StatusApplied(BattleEffectContext context, int duration, int value = 0) => new()
    {
        EventType = "STATUS_APPLIED",
        ActorId = context.Actor.Id,
        TargetId = context.Target.Id,
        SkillId = context.Skill.Id,
        EffectTypeCode = context.Effect.EffectTypeCode,
        Value = value,
        RemainingTurns = duration
    };
}
