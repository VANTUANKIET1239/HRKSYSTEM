namespace GAME.Domain.Battle.Effects;

public sealed class BattleEffectHandlerRegistry
{
    private readonly IReadOnlyDictionary<string, IBattleEffectHandler> _handlers;

    public BattleEffectHandlerRegistry(IEnumerable<IBattleEffectHandler> handlers)
    {
        _handlers = handlers.ToDictionary(x => x.EffectTypeCode, StringComparer.OrdinalIgnoreCase);
    }

    public IBattleEffectHandler GetRequired(string effectTypeCode) =>
        _handlers.TryGetValue(effectTypeCode, out var handler)
            ? handler
            : throw new NotSupportedException($"No battle effect handler is registered for '{effectTypeCode}'.");

    public static BattleEffectHandlerRegistry CreateDefault() => new IBattleEffectHandler[]
    {
        new DamageEffectHandler(),
        new HealEffectHandler(),
        new StatBuffEffectHandler(),
        new StatDebuffEffectHandler()
    }.ToRegistry();
}

internal static class BattleEffectHandlerRegistryExtensions
{
    public static BattleEffectHandlerRegistry ToRegistry(this IEnumerable<IBattleEffectHandler> handlers) => new(handlers);
}
