using GAME.Application.Templates;
using GAME.Infrastructure.Templates;
using Microsoft.Extensions.Options;

namespace HRK.GAME.Common;

public sealed class TemplateRefreshAccess(IHttpContextAccessor http, IWebHostEnvironment environment,
    IOptions<GameTemplateCacheOptions> options) : IGameTemplateRefreshAccess
{
    public bool CanRefresh => options.Value.Enabled && http.HttpContext?.User is { Identity.IsAuthenticated: true } user &&
        (environment.IsDevelopment() || (options.Value.AllowManualRefresh && user.IsInRole("Admin")));
}
