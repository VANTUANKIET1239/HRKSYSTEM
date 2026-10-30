namespace GAME.Domain.Battle.Effects;

public interface ITurnEndEffectHandler
{
    IReadOnlyList<PendingBattleEvent> OnTurnEnd(BattleStatusEffect status, BattleCombatant actor, int round, int turn, IReadOnlyList<BattleCombatant> combatants);
}
