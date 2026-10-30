namespace GAME.Domain.Battle.Effects;

internal static class BattleEffectHandlerRegistryExtensions
{
    public static BattleEffectHandlerRegistry ToRegistry(this IEnumerable<IBattleEffectHandler> handlers) => new(handlers);
}
