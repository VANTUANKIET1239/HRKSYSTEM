namespace GAME.Domain.Battle.Effects;

public sealed class DamageReflectionEffectHandler : TimedStatusEffectHandlerBase
{
    public override string EffectTypeCode => BattleCodes.DamageReflection;
}
