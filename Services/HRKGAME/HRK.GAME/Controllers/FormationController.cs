using GAME.Application.DTOs;
using HRK.GAME.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using GAME.Application.Features.Queries.Formation;
using GAME.Application.Features.Commands.Formation;

namespace HRK.GAME.Controllers;

[Authorize]
[Route("api/formations")]
[Route("api/formation")]
public sealed class FormationController : HRKControllerBase
{
    private readonly IMediator _mediator;
    private string UserId => RequireUserId();

    public FormationController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [HttpGet("list")]
    public async Task<IActionResult> GetFormations(CancellationToken cancellationToken)
    {
        await _mediator.Send(new InitializePlayerFormationsCommand(UserId), cancellationToken);
        return HrkOk(await _mediator.Send(new GetPlayerFormationsQuery(UserId), cancellationToken));
    }

    [HttpGet("{formationCode}")]
    public async Task<IActionResult> GetFormationDetail(string formationCode, CancellationToken cancellationToken)
    {
        await _mediator.Send(new InitializePlayerFormationsCommand(UserId), cancellationToken);
        return HrkOk(await _mediator.Send(new GetFormationDetailQuery(UserId, formationCode), cancellationToken));
    }

    [HttpPut("{formationCode}/positions")]
    public async Task<IActionResult> UpdatePositions(string formationCode, [FromBody] UpdateFormationPositionsRequest request, CancellationToken cancellationToken)
    {
        return HrkOk(await _mediator.Send(new UpdatePositionsCommand(UserId, formationCode, request), cancellationToken));
    }

    [HttpPut("{formationCode}/select")]
    public async Task<IActionResult> SelectFormation(string formationCode, CancellationToken cancellationToken)
    {
        return HrkOk(await _mediator.Send(new SelectFormationCommand(UserId, formationCode), cancellationToken));
    }

    [HttpPost("{formationCode}/upgrade")]
    public async Task<IActionResult> UpgradeFormation(string formationCode, CancellationToken cancellationToken)
    {
        return HrkOk(await _mediator.Send(new UpgradeFormationCommand(UserId, formationCode), cancellationToken));
    }

    [HttpGet("main")]
    public async Task<IActionResult> GetLegacyFormation(CancellationToken cancellationToken)
    {
        await _mediator.Send(new InitializePlayerFormationsCommand(UserId), cancellationToken);
        return HrkOk(await _mediator.Send(new GetPlayerFormationQuery("Main Team"), cancellationToken));
    }
}
