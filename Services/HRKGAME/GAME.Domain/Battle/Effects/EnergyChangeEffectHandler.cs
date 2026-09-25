namespace GAME.Domain.Battle.Effects;

public sealed class EnergyChangeEffectHandler : IBattleEffectHandler
{
    public string EffectTypeCode => BattleCodes.EnergyChange;

    public IReadOnlyList<PendingBattleEvent> Apply(BattleEffectContext context)
    {
        var amount = context.Effect.GetInt("ENERGY_GAIN", (int)Math.Round(context.Effect.BaseValue));
        if (amount == 0 && context.Effect.BaseValue != 0m)
        {
            amount = (int)Math.Round(context.Effect.BaseValue);
        }

        var energyBefore = context.Target.Energy;
        context.Target.Energy = Math.Clamp(context.Target.Energy + amount, 0, context.Target.MaxEnergy);

        return
        [
            new PendingBattleEvent
            {
                EventType = BattleCodes.EnergyChanged,
                ActorId = context.Actor.Id,
                TargetId = context.Target.Id,
                SkillId = context.Skill.Id,
                EffectTypeCode = BattleCodes.EnergyChange,
                Value = amount,
                EnergyBefore = energyBefore,
                EnergyAfter = context.Target.Energy
            }
        ];
    }
}
