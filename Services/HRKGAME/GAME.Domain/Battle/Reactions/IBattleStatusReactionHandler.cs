using GAME.Domain.Battle.Effects;

namespace GAME.Domain.Battle.Reactions;

public interface IBattleStatusReactionHandler
{
    string EffectTypeCode { get; }
    IReadOnlyList<PendingBattleEvent> OnDamaged(BattleStatusEffect status, BattleDamagedContext context);
}
