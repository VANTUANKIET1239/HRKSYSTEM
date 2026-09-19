namespace GAME.Domain.Battle.Effects;

public sealed class HealEffectHandler : IBattleEffectHandler
{
    public string EffectTypeCode => BattleCodes.Heal;

    public IReadOnlyList<PendingBattleEvent> Apply(BattleEffectContext context)
    {
        var amount = Math.Max(1, (int)Math.Round(
            BattleStatCalculator.CalculateEffectValue(context.Effect, context.Actor)));
        var hpBefore = context.Target.Hp;
        context.Target.Hp = Math.Min(context.Target.MaxHp, context.Target.Hp + amount);
        return
        [
            new PendingBattleEvent
            {
                EventType = "HEAL", ActorId = context.Actor.Id, TargetId = context.Target.Id,
                SkillId = context.Skill.Id, EffectTypeCode = context.Effect.EffectTypeCode,
                Value = context.Target.Hp - hpBefore, HpBefore = hpBefore, HpAfter = context.Target.Hp
            }
        ];
    }
}
