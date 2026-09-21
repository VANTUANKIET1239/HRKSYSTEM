namespace GAME.Domain.Battle.Effects;

public sealed class PositionSwapEffectHandler : IBattleEffectHandler
{
    public string EffectTypeCode => BattleCodes.PositionSwap;
    public bool ApplyOncePerEffect => true;

    public IReadOnlyList<PendingBattleEvent> Apply(BattleEffectContext context)
    {
        var targets = context.SelectedTargets.Where(x => x.IsAlive).Take(2).ToList();
        if (targets.Count < 2) return [];

        var firstPosition = targets[0].Position;
        targets[0].Position = targets[1].Position;
        targets[1].Position = firstPosition;

        return targets.Select(target => new PendingBattleEvent
        {
            EventType = "POSITION_CHANGED", ActorId = context.Actor.Id, TargetId = target.Id,
            SkillId = context.Skill.Id, EffectTypeCode = EffectTypeCode, Value = target.Position
        }).ToList();
    }
}
