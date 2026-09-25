using System.Security.Claims;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;
using HRK.GAME.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRK.GAME.Controllers;

[Authorize]
[Route("api/dungeons")]
[Route("api/dungeon")]
public sealed class DungeonController : HRKControllerBase
{
    private readonly IDungeonService _dungeons;
    private readonly IFormationSnapshotService _formationSnapshotService;
    private readonly IGamePlayerService _gamePlayerService;

    public DungeonController(
        IDungeonService dungeons,
        IFormationSnapshotService formationSnapshotService,
        IGamePlayerService gamePlayerService)
    {
        _dungeons = dungeons;
        _formationSnapshotService = formationSnapshotService;
        _gamePlayerService = gamePlayerService;
    }

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? User.FindFirstValue("sub") ?? throw new UnauthorizedAccessException();

    [HttpPost("stages/{stageId:int}/formation-preview")]
    public async Task<IActionResult> FormationPreview(int stageId, [FromBody] FormationPreviewRequestDto request, CancellationToken ct)
    {
        var player = await _gamePlayerService.GetPlayerByUserIdAsync(UserId, ct);
        if (player == null) return NotFoundResponse("Không tìm thấy người chơi.");
        var preview = await _formationSnapshotService.PreviewAsync(player.Id, request.FormationCode, request.Positions, ct);
        return OkResponse(preview);
    }

    [HttpGet("maps")]
    public async Task<IActionResult> Maps(CancellationToken ct) => OkResponse(await _dungeons.GetMapsAsync(UserId, ct));

    [HttpGet("maps/{mapId:int}")]
    public async Task<IActionResult> Map(int mapId, CancellationToken ct) => OkResponse(await _dungeons.GetMapAsync(UserId, mapId, ct));

    [HttpPost("stages/{stageId:int}/start")]
    public async Task<IActionResult> Start(int stageId, [FromBody] StartDungeonStageRequestDto request, CancellationToken ct) =>
        OkResponse(await _dungeons.StartStageAsync(UserId, stageId, request, ct));

    [HttpGet("stamina")]
    public async Task<IActionResult> Stamina(CancellationToken ct) => OkResponse(await _dungeons.GetStaminaAsync(UserId, ct));

    [HttpPost("stamina/purchase")]
    public async Task<IActionResult> PurchaseStamina(CancellationToken ct) => OkResponse(await _dungeons.PurchaseStaminaAsync(UserId, ct));

    [HttpPost("chests/{chestId:int}/claim")]
    public async Task<IActionResult> ClaimChest(int chestId, CancellationToken ct) =>
        OkResponse(await _dungeons.ClaimStarChestAsync(UserId, chestId, ct));
}
