namespace GAME.Domain.Battle.Effects;

public interface IBattleEffectHandler
{
    string EffectTypeCode { get; }
    bool ApplyOncePerEffect => false;
    IReadOnlyList<PendingBattleEvent> Apply(BattleEffectContext context);
}
