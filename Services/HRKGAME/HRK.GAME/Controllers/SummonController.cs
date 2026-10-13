using System.Security.Claims;
using GAME.Application.Features.Commands.Summon;
using GAME.Application.Features.Queries.Summon;
using GAME.Application.DTOs;
using HRK.GAME.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRK.GAME.Controllers;

[Authorize]
[Route("api/summon")]
public sealed class SummonController(IMediator mediator) : HRKControllerBase
{
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new UnauthorizedAccessException();

    [HttpGet("configuration")]
    public async Task<IActionResult> Configuration(CancellationToken ct) =>
        HrkOk(await mediator.Send(new GetSummonConfigurationQuery(UserId), ct));

    [HttpPost]
    public async Task<IActionResult> Summon([FromBody] SummonRequestDto request, CancellationToken ct) =>
        HrkOk(await mediator.Send(new SummonCommand(UserId, request), ct));

    [HttpGet("results/{requestId:guid}")]
    public async Task<IActionResult> Result(Guid requestId, CancellationToken ct) =>
        HrkOk(await mediator.Send(new GetSummonResultQuery(UserId, requestId), ct));
}
