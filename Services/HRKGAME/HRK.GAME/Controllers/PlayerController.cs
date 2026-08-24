using GAME.Application.Features.Queries.Player;
using HRK.GAME.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRK.GAME.Controllers
{
    [Authorize]
    [Route("api/player")]
    public class PlayerController : HRKControllerBase
    {
        private readonly IMediator _mediator;

        public PlayerController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            var response = await _mediator.Send(new GetPlayerProfileQuery());
            return HrkOk(response);
        }

        [HttpGet("wallet")]
        public async Task<IActionResult> GetWallet()
        {
            var response = await _mediator.Send(new GetPlayerWalletQuery());
            return HrkOk(response);
        }
    }
}
