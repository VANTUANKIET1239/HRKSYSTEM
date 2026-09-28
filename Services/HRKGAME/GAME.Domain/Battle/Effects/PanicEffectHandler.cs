namespace GAME.Domain.Battle.Effects;

public sealed class PanicEffectHandler : IBattleEffectHandler
{
    public string EffectTypeCode => BattleCodes.Panic;

    public IReadOnlyList<PendingBattleEvent> Apply(BattleEffectContext context)
    {
        if (context.Effect.DurationTurns <= 0)
        {
            throw new InvalidOperationException(
                $"Skill '{context.Skill.Id}', effect '{EffectTypeCode}' has invalid DurationTurns ({context.Effect.DurationTurns}).");
        }

        var duration = context.Effect.DurationTurns;
        var instanceId = $"{context.Actor.Id}:{context.Skill.Id}:{EffectTypeCode}:{context.Target.Id}";
        var existing = context.Target.StatusEffects.FirstOrDefault(x =>
            x.EffectTypeCode.Equals(EffectTypeCode, StringComparison.OrdinalIgnoreCase));

        var modifiers = context.Effect.StatModifiers?.ToList() ?? [];
        var primaryModValue = modifiers.FirstOrDefault()?.Value ?? context.Effect.BaseValue;

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
                Value = Math.Abs(primaryModValue),
                StatModifiers = modifiers
            });
        }
        else
        {
            existing.RemainingTurns = duration;
            existing.AppliedTurn = context.Turn;
            existing.Value = Math.Abs(primaryModValue);
        }

        return
        [
            new PendingBattleEvent
            {
                EventType = "STATUS_APPLIED",
                ActorId = context.Actor.Id,
                TargetId = context.Target.Id,
                SkillId = context.Skill.Id,
                EffectTypeCode = EffectTypeCode,
                Value = (int)Math.Round(Math.Abs(primaryModValue)),
                RemainingTurns = duration,
                StatModifiers = modifiers
            }
        ];
    }
}
