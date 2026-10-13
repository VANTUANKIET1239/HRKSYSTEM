using GAME.Application.Interfaces;

namespace GAME.Application.Common;

public abstract class AuthenticatedGameHandler(IGameRequestContext requestContext)
{
    protected string GetUserId() => requestContext.UserId;
}
