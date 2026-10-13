using GAME.Application.DTOs;
using HRK.GAME.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using GAME.Application.Features.Queries.Formation;
using GAME.Application.Features.Queries.Dungeon;
using GAME.Application.Features.Commands.Dungeon;

namespace HRK.GAME.Controllers;

[Authorize]
[Route("api/dungeons")]
[Route("api/dungeon")]
public sealed class DungeonController : HRKControllerBase
{
    private readonly IMediator _mediator;
    private string UserId => RequireUserId();

    public DungeonController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("stages/{stageId:int}/formation-preview")]
    public async Task<IActionResult> FormationPreview(int stageId, [FromBody] FormationPreviewRequestDto request, CancellationToken ct)
    {
        return HrkOk(await _mediator.Send(new PreviewFormationQuery(UserId, request.FormationCode, request.Positions), ct));
    }

    [HttpGet("maps")]
    public async Task<IActionResult> Maps(CancellationToken ct)
    {
        return HrkOk(await _mediator.Send(new GetMapsQuery(UserId), ct));
    }

    [HttpGet("maps/{mapId:int}")]
    public async Task<IActionResult> Map(int mapId, CancellationToken ct)
    {
        return HrkOk(await _mediator.Send(new GetMapQuery(UserId, mapId), ct));
    }

    [HttpPost("stages/{stageId:int}/start")]
    public async Task<IActionResult> Start(int stageId, [FromBody] StartDungeonStageRequestDto request, CancellationToken ct)
    {
        return HrkOk(await _mediator.Send(new StartStageCommand(UserId, stageId, request), ct));
    }

    [HttpGet("stamina")]
    public async Task<IActionResult> Stamina(CancellationToken ct)
    {
        return HrkOk(await _mediator.Send(new GetStaminaQuery(UserId), ct));
    }

    [HttpPost("stamina/purchase")]
    public async Task<IActionResult> PurchaseStamina(CancellationToken ct)
    {
        return HrkOk(await _mediator.Send(new PurchaseStaminaCommand(UserId), ct));
    }

    [HttpPost("chests/{chestId:int}/claim")]
    public async Task<IActionResult> ClaimChest(int chestId, CancellationToken ct)
    {
        return HrkOk(await _mediator.Send(new ClaimDungeonChestCommand(UserId, chestId), ct));
    }
}
