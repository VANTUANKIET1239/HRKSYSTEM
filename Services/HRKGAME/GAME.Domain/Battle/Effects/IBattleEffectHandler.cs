namespace GAME.Domain.Battle.Effects;

public interface IBattleEffectHandler
{
    string EffectTypeCode { get; }
    bool ApplyOncePerEffect => false;
    IReadOnlyList<PendingBattleEvent> Apply(BattleEffectContext context);
}

public interface ITurnStartEffectHandler
{
    IReadOnlyList<PendingBattleEvent> OnTurnStart(BattleStatusEffect status, BattleCombatant actor, int round, int turn, IReadOnlyList<BattleCombatant> combatants);
}

public interface ITurnEndEffectHandler
{
    IReadOnlyList<PendingBattleEvent> OnTurnEnd(BattleStatusEffect status, BattleCombatant actor, int round, int turn, IReadOnlyList<BattleCombatant> combatants);
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
    public string? ActionId { get; init; }
    public int TimelineOffsetMs { get; init; }
    public string? PhaseCode { get; init; }
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
    public int? EnergyBefore { get; init; }
    public int? EnergyAfter { get; init; }
    public bool IsCrit { get; init; }
    public int? RemainingTurns { get; init; }
    public int? PreviousStacks { get; init; }
    public int? CurrentStacks { get; init; }
    public int? MaxStacks { get; init; }
    public IReadOnlyList<BattleStatModifier> StatModifiers { get; init; } = [];
    public string? ExecutionGroup { get; init; }
    public int? HitIndex { get; init; }
    public string? ResourceCode { get; init; }
    public int? PreviousValue { get; init; }
    public int? CurrentValue { get; init; }
    public string? ReasonCode { get; init; }
    public string? ActionId { get; init; }
    public string? StatusInstanceId { get; init; }
    public int? TimelineOffsetMs { get; init; }
    public string? PhaseCode { get; init; }
    public long? SourceHeroId { get; init; }
    public int? OriginalDamage { get; init; }
    public int? RedirectRequested { get; init; }
    public int? RedirectActual { get; init; }
    public int? AllyDamageAfterRedirect { get; init; }
    public int? GuardianHpBefore { get; init; }
    public int? GuardianHpAfter { get; init; }
}
