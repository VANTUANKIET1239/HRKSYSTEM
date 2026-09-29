namespace GAME.Domain.Battle.Reactions;

public sealed class BattleStatusReactionHandlerRegistry
{
    private readonly IReadOnlyDictionary<string, IBattleStatusReactionHandler> _handlers;

    public BattleStatusReactionHandlerRegistry(IEnumerable<IBattleStatusReactionHandler> handlers)
    {
        _handlers = handlers.ToDictionary(x => x.EffectTypeCode, StringComparer.OrdinalIgnoreCase);
    }

    public IBattleStatusReactionHandler? GetHandler(string effectTypeCode) =>
        _handlers.TryGetValue(effectTypeCode, out var handler) ? handler : null;

    public static BattleStatusReactionHandlerRegistry CreateDefault() =>
        new([
            new RicardoStatusReactionHandler(),
            new TinChiDanhDuReactionHandler(),
            new CatScratchReactionHandler(BattleCodes.CatScratch),
            new CatScratchReactionHandler(BattleCodes.DeepCatScratch)
        ]);
}
