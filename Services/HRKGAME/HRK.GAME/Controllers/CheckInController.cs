using GAME.Application.DTOs;
using GAME.Application.Features.Commands.CheckIn;
using GAME.Application.Features.Queries.CheckIn;
using Core.Application.CQRS;
using HRK.GAME.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRK.GAME.Controllers;

[Authorize]
[Route("api/check-in")]
public sealed class CheckInController(ICqrsDispatcher dispatcher) : HRKControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct) =>
        HrkOk(await dispatcher.QueryAsync(new GetCheckInCalendarQuery(RequireUserId()), ct));

    [HttpPost("claim")]
    public async Task<IActionResult> Claim([FromBody] ClaimCheckInRequestDto request, CancellationToken ct) =>
        HrkOk(await dispatcher.CommandAsync(new ClaimCheckInCommand(RequireUserId(), request.RequestId), ct));
}
