using GAME.Application.Features.Queries.Battle;
using Core.Web.Compression;
using GAME.Application.DTOs;
using GAME.Application.Features.Commands.Battle;
using HRK.GAME.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRK.GAME.Controllers
{
    [Authorize]
    [Route("api/battle")]
    public class BattleController : HRKControllerBase
    {
        private readonly IMediator _mediator;
        public BattleController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost("start")]
        [HrkResponseCompression("BattleReplay")]
        public async Task<IActionResult> Start([FromBody] StartBattleRequestDto request)
        {
            var response = await _mediator.Send(new StartBattleCommand(request), HttpContext.RequestAborted);
            return HrkOk(response);
        }

        [HttpGet("initial-state")]
        public async Task<IActionResult> GetInitialState()
        {
            var response = await _mediator.Send(new GetBattleInitialStateQuery(), HttpContext.RequestAborted);
            return HrkOk(response);
        }

        [HttpGet("logs")]
        [HrkResponseCompression("BattleReplay")]
        public async Task<IActionResult> GetBattleLogs([FromQuery] string battleId)
        {
            var response = await _mediator.Send(new GetBattleLogsQuery(battleId), HttpContext.RequestAborted);
            return HrkOk(response);
        }
    }
}
