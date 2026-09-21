namespace GAME.Domain.Battle.Effects;

public abstract class StatModifierEffectHandlerBase : IBattleEffectHandler
{
    public abstract string EffectTypeCode { get; }

    public IReadOnlyList<PendingBattleEvent> Apply(BattleEffectContext context)
    {
        var effect = context.Effect;
        if (effect.DurationTurns <= 0 || effect.StatModifiers.Count == 0)
            return [];

        var instanceId = $"{context.Actor.Id}:{context.Skill.Id}:{effect.EffectTypeCode}:{context.Target.Id}";
        var existing = context.Target.StatusEffects.FirstOrDefault(x => x.InstanceId == instanceId);
        if (existing == null)
        {
            context.Target.StatusEffects.Add(new BattleStatusEffect
            {
                InstanceId = instanceId,
                EffectTypeCode = effect.EffectTypeCode,
                SourceSkillId = context.Skill.Id,
                SourceHeroId = context.Actor.Id,
                RemainingTurns = effect.DurationTurns,
                AppliedTurn = context.Turn,
                MaxStacks = Math.Max(1, effect.MaxStacks),
                StatModifiers = effect.StatModifiers
            });
        }
        else
        {
            existing.RemainingTurns = effect.DurationTurns;
            existing.Stacks = Math.Min(existing.MaxStacks, existing.Stacks + 1);
        }

        return
        [
            new PendingBattleEvent
            {
                EventType = "STATUS_APPLIED", ActorId = context.Actor.Id, TargetId = context.Target.Id,
                SkillId = context.Skill.Id, EffectTypeCode = effect.EffectTypeCode,
                RemainingTurns = effect.DurationTurns
            }
        ];
    }
}
