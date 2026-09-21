namespace GAME.Domain.Battle.Effects;

public sealed class StunEffectHandler : TimedStatusEffectHandlerBase
{
    public override string EffectTypeCode => BattleCodes.Stun;
}

public sealed class SilenceEffectHandler : TimedStatusEffectHandlerBase
{
    public override string EffectTypeCode => BattleCodes.Silence;
}

public sealed class TauntEffectHandler : TimedStatusEffectHandlerBase
{
    public override string EffectTypeCode => BattleCodes.Taunt;
}

public sealed class MarkEffectHandler : TimedStatusEffectHandlerBase
{
    public override string EffectTypeCode => BattleCodes.Mark;
    protected override decimal DefaultValue => 20m;
}

public sealed class DamageReductionEffectHandler : TimedStatusEffectHandlerBase
{
    public override string EffectTypeCode => BattleCodes.DamageReduction;
}

public sealed class DamageReflectionEffectHandler : TimedStatusEffectHandlerBase
{
    public override string EffectTypeCode => BattleCodes.DamageReflection;
}
