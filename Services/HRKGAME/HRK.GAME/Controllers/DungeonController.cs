using GAME.Application.DTOs;
using Core.Web.Compression;
using HRK.GAME.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Core.Application.CQRS;
using GAME.Application.Features.Queries.Formation;
using GAME.Application.Features.Queries.Dungeon;
using GAME.Application.Features.Commands.Dungeon;

namespace HRK.GAME.Controllers;

[Authorize]
[Route("api/dungeons")]
[Route("api/dungeon")]
public sealed class DungeonController : HRKControllerBase
{
    private readonly ICqrsDispatcher _dispatcher;
    private string UserId => RequireUserId();

    public DungeonController(ICqrsDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    [HttpPost("stages/{stageId:int}/formation-preview")]
    public async Task<IActionResult> FormationPreview(int stageId, [FromBody] FormationPreviewRequestDto request, CancellationToken ct)
    {
        return HrkOk(await _dispatcher.QueryAsync(new PreviewFormationQuery(UserId, request.FormationCode, request.Positions), ct));
    }

    [HttpGet("maps")]
    public async Task<IActionResult> Maps(CancellationToken ct)
    {
        return HrkOk(await _dispatcher.QueryAsync(new GetMapsQuery(UserId), ct));
    }

    [HttpGet("maps/{mapId:int}")]
    public async Task<IActionResult> Map(int mapId, CancellationToken ct)
    {
        return HrkOk(await _dispatcher.QueryAsync(new GetMapQuery(UserId, mapId), ct));
    }

    [HttpPost("stages/{stageId:int}/start")]
    [HrkResponseCompression("BattleReplay")]
    public async Task<IActionResult> Start(int stageId, [FromBody] StartDungeonStageRequestDto request, CancellationToken ct)
    {
        return HrkOk(await _dispatcher.CommandAsync(new StartStageCommand(UserId, stageId, request), ct));
    }

    [HttpGet("stamina")]
    public async Task<IActionResult> Stamina(CancellationToken ct)
    {
        return HrkOk(await _dispatcher.QueryAsync(new GetStaminaQuery(UserId), ct));
    }

    [HttpPost("stamina/purchase")]
    public async Task<IActionResult> PurchaseStamina(CancellationToken ct, [FromBody(EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] PurchaseStaminaRequest? request = null)
    {
        return HrkOk(await _dispatcher.CommandAsync(new PurchaseStaminaCommand(UserId, request?.RequestId ?? default), ct));
    }

    [HttpPost("chests/{chestId:int}/claim")]
    public async Task<IActionResult> ClaimChest(int chestId, CancellationToken ct)
    {
        return HrkOk(await _dispatcher.CommandAsync(new ClaimDungeonChestCommand(UserId, chestId), ct));
    }
}
