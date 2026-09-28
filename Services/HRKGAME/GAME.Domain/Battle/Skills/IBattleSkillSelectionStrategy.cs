namespace GAME.Domain.Battle.Skills;

public interface IBattleSkillSelectionStrategy
{
    bool CanHandle(BattleCombatant actor);
    BattleSkill SelectSkill(BattleCombatant actor, IReadOnlyList<BattleCombatant> combatants);
}
