using GAME.Domain.Battle.Effects;

namespace GAME.Domain.Battle.Skills.QaKyTinh;

public sealed class FatalAllInSkillHandler : ISkillHandler
{
    public const string SkillId = "FATAL_ALL_IN_DIRECTIVE";
    private readonly DefaultSkillHandler _defaultHandler;

    public FatalAllInSkillHandler(DefaultSkillHandler defaultHandler) => _defaultHandler = defaultHandler;

    public bool CanHandle(BattleSkill skill) => skill.Id.Equals(SkillId, StringComparison.OrdinalIgnoreCase);

    public SkillExecutionResult Execute(SkillExecutionContext context)
    {
        var result = _defaultHandler.Execute(context, effect =>
            !effect.EffectTypeCode.Equals(BattleCodes.StatDebuff, StringComparison.OrdinalIgnoreCase) &&
            !effect.EffectTypeCode.Equals(BattleCodes.Silence, StringComparison.OrdinalIgnoreCase));
        if (!context.Actor.IsAlive) return result;

        if (result.DefeatedTargetIds.Count >= 2)
            ApplyJackpot(context, result);
        else
            ApplyBankruptcy(context, result);
        return result;
    }

    private static void ApplyJackpot(SkillExecutionContext context, SkillExecutionResult result)
    {
        var actor = context.Actor;
        var hpBefore = actor.Hp;
        actor.Hp = Math.Min(actor.MaxHp, actor.Hp + Math.Max(1, (int)Math.Round(actor.MaxHp * 0.20m)));
        result.Events.Add(new PendingBattleEvent
        {
            EventType = "HEAL", ActorId = actor.Id, TargetId = actor.Id, SkillId = context.Skill.Id,
            EffectTypeCode = BattleCodes.Heal, Value = actor.Hp - hpBefore, HpBefore = hpBefore, HpAfter = actor.Hp
        });

        var instanceId = $"{actor.Id}:{context.Skill.Id}:JACKPOT_ATK:{actor.Id}";
        actor.StatusEffects.RemoveAll(x => x.InstanceId == instanceId);
        actor.StatusEffects.Add(new BattleStatusEffect
        {
            InstanceId = instanceId, EffectTypeCode = BattleCodes.StatBuff,
            SourceSkillId = context.Skill.Id, SourceHeroId = actor.Id,
            RemainingTurns = 2, AppliedTurn = context.Turn,
            StatModifiers = [new BattleStatModifier("ATK", "PERCENT", 30m)]
        });
        result.Events.Add(new PendingBattleEvent
        {
            EventType = "STATUS_APPLIED", ActorId = actor.Id, TargetId = actor.Id,
            SkillId = context.Skill.Id, EffectTypeCode = BattleCodes.StatBuff,
            Value = 30, RemainingTurns = 2,
            StatModifiers = [new BattleStatModifier("ATK", "PERCENT", 30m)]
        });
    }

    private void ApplyBankruptcy(SkillExecutionContext context, SkillExecutionResult result)
    {
        foreach (var effect in context.Skill.Effects.Where(x =>
            x.EffectTypeCode.Equals(BattleCodes.StatDebuff, StringComparison.OrdinalIgnoreCase) ||
            x.EffectTypeCode.Equals(BattleCodes.Silence, StringComparison.OrdinalIgnoreCase)))
            result.Events.AddRange(_defaultHandler.ApplyToActor(context, effect));
    }
}
