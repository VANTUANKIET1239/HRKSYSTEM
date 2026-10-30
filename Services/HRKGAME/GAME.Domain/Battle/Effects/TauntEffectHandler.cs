namespace GAME.Domain.Battle.Effects;

public sealed class TauntEffectHandler : TimedStatusEffectHandlerBase
{
    public override string EffectTypeCode => BattleCodes.Taunt;
}
