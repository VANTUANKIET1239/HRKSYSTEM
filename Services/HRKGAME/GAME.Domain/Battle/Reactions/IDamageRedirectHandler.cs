using GAME.Domain.Battle.Effects;
using GAME.Domain.Battle.Skills.NghiaPhucPrime;

namespace GAME.Domain.Battle.Reactions;

public interface IDamageRedirectHandler
{
    DamageRedirectResult? HandleRedirect(DamageRedirectContext context);
}
