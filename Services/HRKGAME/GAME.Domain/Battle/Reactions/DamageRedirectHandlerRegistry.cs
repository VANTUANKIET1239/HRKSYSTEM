using GAME.Domain.Battle.Effects;
using GAME.Domain.Battle.Skills.NghiaPhucPrime;

namespace GAME.Domain.Battle.Reactions;

public sealed class DamageRedirectHandlerRegistry
{
    private readonly List<IDamageRedirectHandler> _handlers;

    public DamageRedirectHandlerRegistry(IEnumerable<IDamageRedirectHandler> handlers)
    {
        _handlers = handlers.ToList();
    }

    public IReadOnlyList<IDamageRedirectHandler> Handlers => _handlers;

    public static DamageRedirectHandlerRegistry CreateDefault() => new([
        new NghiaPhucPrimeReactionHandler()
    ]);
}
