namespace GAME.Domain.Battle.Effects;

public sealed class ActionBarChangeEffectHandler : IBattleEffectHandler
{
    public string EffectTypeCode => BattleCodes.ActionBarChange;

    public IReadOnlyList<PendingBattleEvent> Apply(BattleEffectContext context)
    {
        var delta = context.Effect.GetInt("ACTION_BAR_DELTA", (int)Math.Round(context.Effect.BaseValue));
        if (delta == 0 && context.Effect.BaseValue != 0m)
        {
            delta = (int)Math.Round(context.Effect.BaseValue);
        }

        var prevEnergy = context.Target.Energy;
        var newEnergy = Math.Clamp(prevEnergy + delta, 0, context.Target.MaxEnergy);
        context.Target.Energy = newEnergy;
        var actualChange = newEnergy - prevEnergy;

        return
        [
            new PendingBattleEvent
            {
                EventType = BattleCodes.ActionBarChanged,
                ActorId = context.Actor.Id,
                TargetId = context.Target.Id,
                SkillId = context.Skill.Id,
                EffectTypeCode = BattleCodes.ActionBarChange,
                Value = actualChange,
                PreviousValue = prevEnergy,
                CurrentValue = newEnergy,
                EnergyBefore = prevEnergy,
                EnergyAfter = newEnergy,
                TimelineOffsetMs = context.Effect.GetInt("TIMELINE_OFFSET_MS", 0),
                PhaseCode = "IMPACT"
            }
        ];
    }
}
