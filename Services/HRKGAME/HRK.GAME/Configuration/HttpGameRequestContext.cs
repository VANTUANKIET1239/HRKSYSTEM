using GAME.Application.Interfaces;
using System.Security.Claims;

namespace HRK.GAME.Configuration;

public sealed class HttpGameRequestContext(IHttpContextAccessor accessor) : IGameRequestContext
{
    public string UserId
    {
        get
        {
            var user = accessor.HttpContext?.User;
            var id = user?.FindFirstValue(ClaimTypes.NameIdentifier) ?? user?.FindFirstValue("sub") ?? user?.FindFirstValue("UserId") ?? user?.FindFirstValue("id");
            return string.IsNullOrWhiteSpace(id) ? throw new UnauthorizedAccessException() : id;
        }
    }
}
