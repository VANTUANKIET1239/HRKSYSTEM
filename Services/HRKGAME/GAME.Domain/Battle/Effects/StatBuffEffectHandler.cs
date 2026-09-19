namespace GAME.Domain.Battle.Effects;

public sealed class StatBuffEffectHandler : StatModifierEffectHandlerBase
{
    public override string EffectTypeCode => BattleCodes.StatBuff;
}
