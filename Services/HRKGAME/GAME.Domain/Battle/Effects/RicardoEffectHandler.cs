namespace GAME.Domain.Battle.Effects;

public sealed class RicardoEffectHandler : IBattleEffectHandler
{
    public string EffectTypeCode => BattleCodes.Ricardo;

    public IReadOnlyList<PendingBattleEvent> Apply(BattleEffectContext context)
    {
        var effect = context.Effect;
        var existing = context.Target.StatusEffects.FirstOrDefault(x =>
            x.EffectTypeCode.Equals(BattleCodes.Ricardo, StringComparison.OrdinalIgnoreCase) &&
            (x.RemainingTurns > 0 || x.RemainingTurns == -1));

        if (existing == null)
        {
            var initialStacks = effect.GetInt("RICARDO_INITIAL_STACKS", 1);
            var maxStacks = effect.GetInt("RICARDO_MAX_STACKS", 6);
            var damageBonusPerStack = effect.GetDecimal("RICARDO_DAMAGE_PER_STACK_PERCENT", 10m);
            var duration = effect.DurationTurns <= 0 ? -1 : effect.DurationTurns;

            var instanceId = $"{context.Actor.Id}:{context.Skill.Id}:{BattleCodes.Ricardo}:{context.Target.Id}";
            var status = new BattleStatusEffect
            {
                InstanceId = instanceId,
                EffectTypeCode = BattleCodes.Ricardo,
                SourceSkillId = context.Skill.Id,
                SourceHeroId = context.Actor.Id,
                RemainingTurns = duration,
                AppliedTurn = context.Turn,
                Stacks = initialStacks,
                MaxStacks = maxStacks,
                Value = damageBonusPerStack,
                DamageBonusPerStackPercent = damageBonusPerStack,
                ScaleModifiersWithStacks = false, // DEF +30% & MAGIC_RESISTANCE +30% do not scale with stacks
                StatModifiers = effect.StatModifiers
            };

            context.Target.StatusEffects.Add(status);

            return
            [
                new PendingBattleEvent
                {
                    EventType = "STATUS_APPLIED",
                    ActorId = context.Actor.Id,
                    TargetId = context.Target.Id,
                    SkillId = context.Skill.Id,
                    EffectTypeCode = BattleCodes.Ricardo,
                    Value = initialStacks,
                    RemainingTurns = duration == -1 ? null : duration,
                    PreviousStacks = 0,
                    CurrentStacks = initialStacks,
                    MaxStacks = maxStacks,
                    StatModifiers = effect.StatModifiers
                },
                new PendingBattleEvent
                {
                    EventType = BattleCodes.RicardoApplied,
                    ActorId = context.Actor.Id,
                    TargetId = context.Target.Id,
                    SkillId = context.Skill.Id,
                    EffectTypeCode = BattleCodes.Ricardo,
                    Value = initialStacks,
                    RemainingTurns = duration == -1 ? null : duration,
                    PreviousStacks = 0,
                    CurrentStacks = initialStacks,
                    MaxStacks = maxStacks,
                    StatModifiers = effect.StatModifiers
                }
            ];
        }
        else
        {
            // Reapply does not reset stacks and does not add stacks
            if (context.Effect.DurationTurns > 0)
            {
                existing.RemainingTurns = context.Effect.DurationTurns;
            }

            return
            [
                new PendingBattleEvent
                {
                    EventType = BattleCodes.StatusRefreshed,
                    ActorId = context.Actor.Id,
                    TargetId = context.Target.Id,
                    SkillId = context.Skill.Id,
                    EffectTypeCode = BattleCodes.Ricardo,
                    Value = existing.Stacks,
                    RemainingTurns = existing.RemainingTurns == -1 ? null : existing.RemainingTurns,
                    PreviousStacks = existing.Stacks,
                    CurrentStacks = existing.Stacks,
                    MaxStacks = existing.MaxStacks,
                    StatModifiers = existing.StatModifiers
                }
            ];
        }
    }
}
