namespace GAME.Domain.Battle.Effects;

public sealed class StunEffectHandler : TimedStatusEffectHandlerBase
{
    public override string EffectTypeCode => BattleCodes.Stun;
}
