namespace GAME.Domain.Battle.Effects;

public abstract class StatModifierEffectHandlerBase : IBattleEffectHandler
{
    public abstract string EffectTypeCode { get; }

    public IReadOnlyList<PendingBattleEvent> Apply(BattleEffectContext context)
    {
        var effect = context.Effect;
        if (effect.DurationTurns <= 0 || effect.StatModifiers.Count == 0)
            return [];

        var val = effect.BaseValue != 0 ? effect.BaseValue : (effect.StatModifiers.FirstOrDefault()?.Value ?? 0m);
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
                Value = val,
                StatModifiers = effect.StatModifiers
            });
        }
        else
        {
            existing.RemainingTurns = effect.DurationTurns;
            existing.Stacks = Math.Min(existing.MaxStacks, existing.Stacks + 1);
            existing.Value = val;
        }

        return
        [
            new PendingBattleEvent
            {
                EventType = "STATUS_APPLIED", ActorId = context.Actor.Id, TargetId = context.Target.Id,
                SkillId = context.Skill.Id, EffectTypeCode = effect.EffectTypeCode,
                Value = (int)Math.Round(val),
                RemainingTurns = effect.DurationTurns, StatModifiers = effect.StatModifiers
            }
        ];
    }
}
