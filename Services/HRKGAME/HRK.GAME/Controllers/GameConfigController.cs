using HRK.GAME.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Core.Application.CQRS;
using GAME.Application.Features.Queries.GameConfig;
using GAME.Application.Features.Commands.GameConfig;

namespace HRK.GAME.Controllers;

[Authorize]
[Route("api/game-config")]
public sealed class GameConfigController : HRKControllerBase
{
    private readonly ICqrsDispatcher _dispatcher;
    private string UserId => RequireUserId();

    public GameConfigController(ICqrsDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    [HttpGet("features")]
    public async Task<IActionResult> GetFeatures(CancellationToken cancellationToken)
    {
        return HrkOk(await _dispatcher.QueryAsync(new GetGameFeaturesQuery(), cancellationToken));
    }

    [HttpGet("templates/capabilities")]
    public async Task<IActionResult> GetTemplateCacheCapabilities(CancellationToken cancellationToken) =>
        HrkOk(await _dispatcher.QueryAsync(new GetTemplateCacheCapabilitiesQuery(), cancellationToken));

    [HttpPost("templates/refresh")]
    public async Task<IActionResult> RefreshTemplates(CancellationToken cancellationToken) =>
        HrkOk(await _dispatcher.CommandAsync(new RefreshTemplateCacheCommand(), cancellationToken));

    [HttpGet("combat-power")]
    public async Task<IActionResult> GetCombatPowerConfigs(CancellationToken cancellationToken)
    {
        return HrkOk(await _dispatcher.QueryAsync(new GetCombatPowerConfigsQuery(), cancellationToken));
    }

    [HttpGet("modals")]
    public async Task<IActionResult> GetModalConfigs(CancellationToken cancellationToken)
    {
        return HrkOk(await _dispatcher.QueryAsync(new GetModalConfigsQuery(), cancellationToken));
    }
}
