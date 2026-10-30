using GAME.Domain.Battle.Effects;

namespace GAME.Domain.Battle.Skills;

public sealed class SkillExecutionContext
{
    public required BattleSkill Skill { get; init; }
    public required BattleCombatant Actor { get; init; }
    public required List<BattleCombatant> Combatants { get; init; }
    public required Random Random { get; init; }
    public int Round { get; init; }
    public int Turn { get; init; }
    public string? ActionId { get; init; }
}
