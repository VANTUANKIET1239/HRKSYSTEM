namespace GAME.Domain.Battle.Effects;

public interface ITurnStartEffectHandler
{
    IReadOnlyList<PendingBattleEvent> OnTurnStart(BattleStatusEffect status, BattleCombatant actor, int round, int turn, IReadOnlyList<BattleCombatant> combatants);
}
