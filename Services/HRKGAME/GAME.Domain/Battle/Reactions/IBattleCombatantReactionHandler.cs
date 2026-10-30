using GAME.Domain.Battle.Effects;
using GAME.Domain.Battle.Skills;

namespace GAME.Domain.Battle.Reactions;

public interface IBattleCombatantReactionHandler
{
    IReadOnlyList<PendingBattleEvent> OnTargeted(BattleCombatant target, BattleTargetedReactionContext context);
    IReadOnlyList<PendingBattleEvent> OnCombatantDefeated(BattleCombatant observer, BattleCombatantDefeatedReactionContext context);
    IReadOnlyList<PendingBattleEvent> OnActionCompleted(BattleCombatant actor, BattleActionCompletedReactionContext context) => [];
}
