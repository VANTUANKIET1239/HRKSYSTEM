namespace GAME.Domain.Battle.Effects;

public sealed class DamageEffectHandler : IBattleEffectHandler
{
    public string EffectTypeCode => BattleCodes.Damage;

    public IReadOnlyList<PendingBattleEvent> Apply(BattleEffectContext context)
    {
        var effect = context.Effect;
        var actor = context.Actor;
        var target = context.Target;
        var rawDamage = BattleStatCalculator.CalculateEffectValue(effect, actor);
        var school = effect.DamageSchoolCode ?? BattleCodes.Physical;
        var defense = school == BattleCodes.Magic
            ? BattleStatCalculator.GetEffectiveStat(target, "MAGIC_RESISTANCE")
            : BattleStatCalculator.GetEffectiveStat(target, "DEF");
        var damage = school == BattleCodes.True
            ? Math.Max(1, (int)Math.Round(rawDamage))
            : Math.Max(1, (int)Math.Round(rawDamage * 100m / (100m + Math.Max(0m, defense))));
        var isCrit = (decimal)context.Random.NextDouble() * 100m < actor.CritChance;
        if (isCrit)
            damage = Math.Max(1, (int)Math.Round(damage * actor.CritDamage / 100m));

        var hpBefore = target.Hp;
        target.Hp = Math.Max(0, target.Hp - damage);
        var events = new List<PendingBattleEvent>
        {
            new()
            {
                EventType = "DAMAGE", ActorId = actor.Id, TargetId = target.Id,
                SkillId = context.Skill.Id, EffectTypeCode = effect.EffectTypeCode,
                DamageSchoolCode = school, Value = damage, HpBefore = hpBefore,
                HpAfter = target.Hp, IsCrit = isCrit
            }
        };
        if (hpBefore > 0 && target.Hp == 0)
            events.Add(new PendingBattleEvent
            {
                EventType = "DEATH", ActorId = actor.Id, TargetId = target.Id,
                SkillId = context.Skill.Id
            });
        return events;
    }
}
