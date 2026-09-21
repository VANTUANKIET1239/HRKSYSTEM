namespace GAME.Domain.Battle.Effects;

public interface IBattleEffectHandler
{
    string EffectTypeCode { get; }
    bool ApplyOncePerEffect => false;
    IReadOnlyList<PendingBattleEvent> Apply(BattleEffectContext context);
}

public sealed class BattleEffectContext
{
    public required BattleSkillEffect Effect { get; init; }
    public required BattleSkill Skill { get; init; }
    public required BattleCombatant Actor { get; init; }
    public required BattleCombatant Target { get; init; }
    public required IReadOnlyList<BattleCombatant> SelectedTargets { get; init; }
    public required IReadOnlyList<BattleCombatant> Combatants { get; init; }
    public required Random Random { get; init; }
    public int Round { get; init; }
    public int Turn { get; init; }
}

public sealed class PendingBattleEvent
{
    public required string EventType { get; init; }
    public long? ActorId { get; init; }
    public long? TargetId { get; init; }
    public string? SkillId { get; init; }
    public string? EffectTypeCode { get; init; }
    public string? DamageSchoolCode { get; init; }
    public int Value { get; init; }
    public int? HpBefore { get; init; }
    public int? HpAfter { get; init; }
    public bool IsCrit { get; init; }
    public int? RemainingTurns { get; init; }
}
