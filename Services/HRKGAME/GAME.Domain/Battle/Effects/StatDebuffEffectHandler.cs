namespace GAME.Domain.Battle.Effects;

public sealed class StatDebuffEffectHandler : StatModifierEffectHandlerBase
{
    public override string EffectTypeCode => BattleCodes.StatDebuff;
}
