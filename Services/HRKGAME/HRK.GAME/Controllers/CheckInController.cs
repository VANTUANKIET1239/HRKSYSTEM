using GAME.Application.DTOs;
using GAME.Application.Features.Commands.CheckIn;
using GAME.Application.Features.Queries.CheckIn;
using MediatR;
using HRK.GAME.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRK.GAME.Controllers;

[Authorize]
[Route("api/check-in")]
public sealed class CheckInController(IMediator mediator) : HRKControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct) =>
        HrkOk(await mediator.Send(new GetCheckInCalendarQuery(RequireUserId()), ct));

    [HttpPost("claim")]
    public async Task<IActionResult> Claim([FromBody] ClaimCheckInRequestDto request, CancellationToken ct) =>
        HrkOk(await mediator.Send(new ClaimCheckInCommand(RequireUserId(), request.RequestId), ct));
}
