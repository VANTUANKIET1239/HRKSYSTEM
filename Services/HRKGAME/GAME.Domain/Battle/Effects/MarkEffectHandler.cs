namespace GAME.Domain.Battle.Effects;

public sealed class MarkEffectHandler : TimedStatusEffectHandlerBase
{
    public override string EffectTypeCode => BattleCodes.Mark;
    protected override decimal DefaultValue => 20m;
}
