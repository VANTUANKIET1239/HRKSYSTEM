namespace GAME.Domain.Battle.Effects;

public sealed class SilenceEffectHandler : TimedStatusEffectHandlerBase
{
    public override string EffectTypeCode => BattleCodes.Silence;
}
