namespace GAME.Domain.Battle.Effects;

public sealed class DamageReductionEffectHandler : TimedStatusEffectHandlerBase
{
    public override string EffectTypeCode => BattleCodes.DamageReduction;
}
