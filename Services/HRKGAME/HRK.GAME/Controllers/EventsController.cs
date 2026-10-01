using System.Security.Claims;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;
using HRK.GAME.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRK.GAME.Controllers;

[Authorize]
[Route("api/events")]
public sealed class EventsController : HRKControllerBase
{
    private readonly ITowerClimbService _towerClimbService;
    private readonly ITowerQuickClimbService _quickClimbService;
    private readonly IFormationSnapshotService _formationSnapshotService;
    private readonly IGamePlayerService _gamePlayerService;

    public EventsController(
        ITowerClimbService towerClimbService,
        ITowerQuickClimbService quickClimbService,
        IFormationSnapshotService formationSnapshotService,
        IGamePlayerService gamePlayerService)
    {
        _towerClimbService = towerClimbService;
        _quickClimbService = quickClimbService;
        _formationSnapshotService = formationSnapshotService;
        _gamePlayerService = gamePlayerService;
    }

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? User.FindFirstValue("sub") ?? throw new UnauthorizedAccessException();

    [HttpGet("")]
    public async Task<IActionResult> GetEvents(CancellationToken ct) =>
        OkResponse(await _towerClimbService.GetEventsAsync(UserId, ct));

    [HttpGet("{eventCode}")]
    public async Task<IActionResult> GetEventDetail(string eventCode, CancellationToken ct) =>
        OkResponse(await _towerClimbService.GetEventDetailAsync(UserId, eventCode, ct));

    [HttpGet("tower/progress")]
    public async Task<IActionResult> GetTowerProgress(CancellationToken ct) =>
        OkResponse(await _towerClimbService.GetPlayerProgressAsync(UserId, ct));

    [HttpGet("tower/floors")]
    public async Task<IActionResult> GetTowerFloors(CancellationToken ct) =>
        OkResponse(await _towerClimbService.GetFloorsAsync(UserId, ct));

    [HttpGet("tower/floors/{floorNumber:int}")]
    public async Task<IActionResult> GetTowerFloorDetail(int floorNumber, CancellationToken ct) =>
        OkResponse(await _towerClimbService.GetFloorDetailAsync(UserId, floorNumber, ct));

    [HttpPost("tower/floors/{floorNumber:int}/formation-preview")]
    public async Task<IActionResult> FormationPreview(int floorNumber, [FromBody] FormationPreviewRequestDto request, CancellationToken ct)
    {
        var player = await _gamePlayerService.GetPlayerByUserIdAsync(UserId, ct);
        if (player == null) return NotFoundResponse("Không tìm thấy người chơi.");
        var preview = await _formationSnapshotService.PreviewAsync(player.Id, request.FormationCode, request.Positions, ct);
        return OkResponse(preview);
    }

    [HttpPost("tower/floors/{floorNumber:int}/start")]
    public async Task<IActionResult> StartFloorBattle(int floorNumber, [FromBody] StartTowerBattleRequestDto request, CancellationToken ct) =>
        OkResponse(await _towerClimbService.StartFloorBattleAsync(UserId, floorNumber, request, ct));

    [HttpPost("tower/new-run")]
    public async Task<IActionResult> StartNewRun(CancellationToken ct) =>
        OkResponse(await _towerClimbService.StartNewRunAsync(UserId, ct));

    [HttpPost("tower/chests/{chestId:int}/claim")]
    public async Task<IActionResult> ClaimChest(int chestId, CancellationToken ct) =>
        OkResponse(await _towerClimbService.ClaimMilestoneChestAsync(UserId, chestId, ct));

    [HttpGet("tower/pending-rewards")]
    public async Task<IActionResult> GetPendingRewards(CancellationToken ct) =>
        OkResponse(await _towerClimbService.GetPendingRewardsAsync(UserId, ct));

    [HttpPost("tower/pending-rewards/{pendingRewardId:long}/claim")]
    public async Task<IActionResult> ClaimPendingReward(long pendingRewardId, CancellationToken ct) =>
        OkResponse(await _towerClimbService.ClaimPendingRewardAsync(UserId, pendingRewardId, ct));

    [HttpPost("tower/quick-climb/start")]
    public async Task<IActionResult> StartQuickClimb([FromBody] StartQuickClimbRequestDto request, CancellationToken ct) =>
        OkResponse(await _quickClimbService.StartQuickClimbAsync(UserId, request, ct));

    [HttpGet("tower/quick-climb/active")]
    public async Task<IActionResult> GetActiveQuickClimb(CancellationToken ct) =>
        OkResponse(await _quickClimbService.GetActiveJobAsync(UserId, ct));

    [HttpGet("tower/quick-climb/{jobId}")]
    public async Task<IActionResult> GetQuickClimbStatus(string jobId, CancellationToken ct) =>
        OkResponse(await _quickClimbService.GetJobStatusAsync(UserId, jobId, ct));

    [HttpPost("tower/quick-climb/{jobId}/stop")]
    public async Task<IActionResult> StopQuickClimb(string jobId, CancellationToken ct) =>
        OkResponse(await _quickClimbService.StopQuickClimbAsync(UserId, jobId, ct));

    [HttpGet("tower/battles/{battleId}")]
    public async Task<IActionResult> GetBattleDetail(string battleId, CancellationToken ct)
    {
        var battle = await _towerClimbService.GetBattleDetailAsync(UserId, battleId, ct);
        if (battle == null) return NotFoundResponse("Không tìm thấy trận đánh.");
        return OkResponse(battle);
    }
}
