using GAME.Domain.Battle.Skills.ThanhThaiAura;

namespace GAME.Domain.Battle.Reactions;

public sealed class BattleCombatantReactionRegistry
{
    private readonly List<IBattleCombatantReactionHandler> _handlers;

    public BattleCombatantReactionRegistry(IEnumerable<IBattleCombatantReactionHandler> handlers)
    {
        _handlers = handlers.ToList();
    }

    public IReadOnlyList<IBattleCombatantReactionHandler> Handlers => _handlers;

    public static BattleCombatantReactionRegistry CreateDefault() =>
        new([new ThanhThaiAuraReactionHandler()]);
}
