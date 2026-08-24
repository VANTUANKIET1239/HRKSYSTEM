using GAME.Application.Features.Queries.Battle;
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

        [HttpGet("initial-state")]
        public async Task<IActionResult> GetInitialState()
        {
            var response = await _mediator.Send(new GetBattleInitialStateQuery());
            return HrkOk(response);
        }

        [HttpGet("logs")]
        public async Task<IActionResult> GetBattleLogs([FromQuery] string battleId)
        {
            var response = await _mediator.Send(new GetBattleLogsQuery(battleId));
            return HrkOk(response);
        }
    }
}
