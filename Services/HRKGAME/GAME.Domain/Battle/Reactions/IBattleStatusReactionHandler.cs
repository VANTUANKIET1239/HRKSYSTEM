using GAME.Domain.Battle.Effects;

namespace GAME.Domain.Battle.Reactions;

public sealed class BattleDamagedContext
{
    public required BattleCombatant Actor { get; init; }
    public required BattleCombatant Target { get; init; }
    public required BattleSkill Skill { get; init; }
    public required BattleSkillEffect Effect { get; init; }
    public int ActualHpDamage { get; init; }
    public int ShieldAbsorbed { get; init; }
    public bool WasHit { get; init; }
    public required string ActionId { get; init; }
    public int Round { get; init; }
    public int Turn { get; init; }
    public required IReadOnlyList<BattleCombatant> Combatants { get; init; }
    public required Random Random { get; init; }
    public int TimelineOffsetMs { get; init; }
    public string? PhaseCode { get; init; }
}

public interface IBattleStatusReactionHandler
{
    string EffectTypeCode { get; }
    IReadOnlyList<PendingBattleEvent> OnDamaged(BattleStatusEffect status, BattleDamagedContext context);
}
