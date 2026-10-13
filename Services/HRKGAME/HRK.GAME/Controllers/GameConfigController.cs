using HRK.GAME.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using GAME.Application.Features.Queries.GameConfig;

namespace HRK.GAME.Controllers;

[Authorize]
[Route("api/game-config")]
public sealed class GameConfigController : HRKControllerBase
{
    private readonly IMediator _mediator;
    private string UserId => RequireUserId();

    public GameConfigController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("features")]
    public async Task<IActionResult> GetFeatures(CancellationToken cancellationToken)
    {
        return HrkOk(await _mediator.Send(new GetGameFeaturesQuery(), cancellationToken));
    }

    [HttpGet("combat-power")]
    public async Task<IActionResult> GetCombatPowerConfigs(CancellationToken cancellationToken)
    {
        return HrkOk(await _mediator.Send(new GetCombatPowerConfigsQuery(), cancellationToken));
    }

    [HttpGet("modals")]
    public async Task<IActionResult> GetModalConfigs(CancellationToken cancellationToken)
    {
        return HrkOk(await _mediator.Send(new GetModalConfigsQuery(), cancellationToken));
    }
}
