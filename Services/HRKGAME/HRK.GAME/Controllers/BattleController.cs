using GAME.Application.Features.Queries.Battle;
using Core.Web.Compression;
using GAME.Application.DTOs;
using GAME.Application.Features.Commands.Battle;
using HRK.GAME.Common;
using Core.Application.CQRS;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRK.GAME.Controllers
{
    [Authorize]
    [Route("api/battle")]
    public class BattleController : HRKControllerBase
    {
        private readonly ICqrsDispatcher _dispatcher;
        public BattleController(ICqrsDispatcher dispatcher)
        {
            _dispatcher = dispatcher;
        }

        [HttpPost("start")]
        [HrkResponseCompression("BattleReplay")]
        public async Task<IActionResult> Start([FromBody] StartBattleRequestDto request)
        {
            var response = await _dispatcher.CommandAsync(new StartBattleCommand(request), HttpContext.RequestAborted);
            return HrkOk(response);
        }

        [HttpGet("initial-state")]
        public async Task<IActionResult> GetInitialState()
        {
            var response = await _dispatcher.QueryAsync(new GetBattleInitialStateQuery(), HttpContext.RequestAborted);
            return HrkOk(response);
        }

        [HttpGet("logs")]
        [HrkResponseCompression("BattleReplay")]
        public async Task<IActionResult> GetBattleLogs([FromQuery] string battleId)
        {
            var response = await _dispatcher.QueryAsync(new GetBattleLogsQuery(battleId), HttpContext.RequestAborted);
            return HrkOk(response);
        }
    }
}
