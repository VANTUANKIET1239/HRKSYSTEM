using GAME.Application.DTOs;
using Core.Web.Compression;
using HRK.GAME.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Core.Application.CQRS;
using GAME.Application.Features.Queries.Tower;
using GAME.Application.Features.Queries.Formation;
using GAME.Application.Features.Commands.Tower;
using GAME.Application.Features.Commands.QuickClimb;
using GAME.Application.Features.Queries.QuickClimb;

namespace HRK.GAME.Controllers;

[Authorize]
[Route("api/events")]
public sealed class EventsController : HRKControllerBase
{
    private readonly ICqrsDispatcher _dispatcher;
    private string UserId => RequireUserId();

    public EventsController(ICqrsDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    [HttpGet("")]
    public async Task<IActionResult> GetEvents(CancellationToken ct)
    {
        await _dispatcher.CommandAsync(new RefreshTowerPeriodCommand(UserId, EventCode: null), ct);
        return HrkOk(await _dispatcher.QueryAsync(new GetEventsQuery(UserId), ct));
    }

    [HttpGet("{eventCode}")]
    public async Task<IActionResult> GetEventDetail(string eventCode, CancellationToken ct)
    {
        await _dispatcher.CommandAsync(new RefreshTowerPeriodCommand(UserId, EventCode: null), ct);
        return HrkOk(await _dispatcher.QueryAsync(new GetEventDetailQuery(UserId, eventCode), ct));
    }

    [HttpGet("tower/progress")]
    public async Task<IActionResult> GetTowerProgress(CancellationToken ct)
    {
        await _dispatcher.CommandAsync(new RefreshTowerPeriodCommand(UserId), ct);
        return HrkOk(await _dispatcher.QueryAsync(new GetPlayerProgressQuery(UserId), ct));
    }

    [HttpGet("tower/floors")]
    public async Task<IActionResult> GetTowerFloors(CancellationToken ct)
    {
        await _dispatcher.CommandAsync(new RefreshTowerPeriodCommand(UserId), ct);
        return HrkOk(await _dispatcher.QueryAsync(new GetFloorsQuery(UserId), ct));
    }

    [HttpGet("tower/floors/{floorNumber:int}")]
    public async Task<IActionResult> GetTowerFloorDetail(int floorNumber, CancellationToken ct)
    {
        await _dispatcher.CommandAsync(new RefreshTowerPeriodCommand(UserId), ct);
        return HrkOk(await _dispatcher.QueryAsync(new GetFloorDetailQuery(UserId, floorNumber), ct));
    }

    [HttpPost("tower/floors/{floorNumber:int}/formation-preview")]
    public async Task<IActionResult> FormationPreview(int floorNumber, [FromBody] FormationPreviewRequestDto request, CancellationToken ct)
    {
        return HrkOk(await _dispatcher.QueryAsync(new PreviewFormationQuery(UserId, request.FormationCode, request.Positions), ct));
    }

    [HttpPost("tower/floors/{floorNumber:int}/start")]
    [HrkResponseCompression("BattleReplay")]
    public async Task<IActionResult> StartFloorBattle(int floorNumber, [FromBody] StartTowerBattleRequestDto request, CancellationToken ct)
    {
        return HrkOk(await _dispatcher.CommandAsync(new StartFloorBattleCommand(UserId, floorNumber, request), ct));
    }

    [HttpPost("tower/new-run")]
    public async Task<IActionResult> StartNewRun(CancellationToken ct)
    {
        return HrkOk(await _dispatcher.CommandAsync(new StartNewRunCommand(UserId), ct));
    }

    [HttpPost("tower/chests/{chestId:int}/claim")]
    public async Task<IActionResult> ClaimChest(int chestId, CancellationToken ct)
    {
        return HrkOk(await _dispatcher.CommandAsync(new ClaimMilestoneChestCommand(UserId, chestId), ct));
    }

    [HttpGet("tower/pending-rewards")]
    public async Task<IActionResult> GetPendingRewards(CancellationToken ct)
    {
        await _dispatcher.CommandAsync(new RefreshTowerPeriodCommand(UserId), ct);
        return HrkOk(await _dispatcher.QueryAsync(new GetPendingRewardsQuery(UserId), ct));
    }

    [HttpPost("tower/pending-rewards/{pendingRewardId:long}/claim")]
    public async Task<IActionResult> ClaimPendingReward(long pendingRewardId, CancellationToken ct)
    {
        return HrkOk(await _dispatcher.CommandAsync(new ClaimPendingRewardCommand(UserId, pendingRewardId), ct));
    }

    [HttpPost("tower/quick-climb/start")]
    public async Task<IActionResult> StartQuickClimb([FromBody] StartQuickClimbRequestDto request, CancellationToken ct)
    {
        return HrkOk(await _dispatcher.CommandAsync(new StartQuickClimbCommand(UserId, request), ct));
    }

    [HttpGet("tower/quick-climb/active")]
    public async Task<IActionResult> GetActiveQuickClimb(CancellationToken ct)
    {
        return HrkOk(await _dispatcher.QueryAsync(new GetActiveJobQuery(UserId), ct));
    }

    [HttpGet("tower/quick-climb/{jobId}")]
    public async Task<IActionResult> GetQuickClimbStatus(string jobId, CancellationToken ct)
    {
        return HrkOk(await _dispatcher.QueryAsync(new GetJobStatusQuery(UserId, jobId), ct));
    }

    [HttpPost("tower/quick-climb/{jobId}/stop")]
    public async Task<IActionResult> StopQuickClimb(string jobId, CancellationToken ct)
    {
        return HrkOk(await _dispatcher.CommandAsync(new StopQuickClimbCommand(UserId, jobId), ct));
    }

    [HttpGet("tower/battles/{battleId}")]
    [HrkResponseCompression("BattleReplay")]
    public async Task<IActionResult> GetBattleDetail(string battleId, CancellationToken ct)
    {
        return HrkOk(await _dispatcher.QueryAsync(new GetBattleDetailQuery(UserId, battleId), ct));
    }
}
