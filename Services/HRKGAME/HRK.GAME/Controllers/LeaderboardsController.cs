using System.ComponentModel.DataAnnotations;
using Core.Application.CQRS;
using Core.Common.Entity.MyCompany.Shared.Responses;
using GAME.Application.DTOs.Leaderboard;
using GAME.Application.Features.Queries.Leaderboard;
using HRK.GAME.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRK.GAME.Controllers;

[Authorize]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[Route("api/leaderboards")]
public sealed class LeaderboardsController(ICqrsDispatcher dispatcher) : HRKControllerBase
{
    /// <summary>Gets a page of lifetime power or diamond-spending records.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(BaseResponse<LeaderboardResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Get(CancellationToken ct, [FromQuery] string type = "power",
        [FromQuery, Range(1, int.MaxValue)] int page = 1, [FromQuery, Range(1, 100)] int pageSize = 20) =>
        HrkOk(await dispatcher.QueryAsync(new GetLeaderboardQuery(type, page, pageSize), ct));

    /// <summary>Inspects the current default formation and public player records.</summary>
    [HttpGet("players/{playerId:long:min(1)}")]
    [ProducesResponseType(typeof(BaseResponse<PublicPlayerProfileResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Profile(long playerId, CancellationToken ct) =>
        HrkOk(await dispatcher.QueryAsync(new GetPublicPlayerProfileQuery(playerId), ct));

    /// <summary>Inspects one hero currently placed in the target player's default formation.</summary>
    [HttpGet("players/{playerId:long:min(1)}/heroes/{heroId:long:min(1)}")]
    [ProducesResponseType(typeof(BaseResponse<PublicHeroDetailResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Hero(long playerId, long heroId, CancellationToken ct) =>
        HrkOk(await dispatcher.QueryAsync(new GetPublicPlayerHeroQuery(playerId, heroId), ct));
}
