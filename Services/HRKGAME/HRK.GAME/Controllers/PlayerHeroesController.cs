using GAME.Application.Features.Queries.PlayerHeroes;
using HRK.GAME.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRK.GAME.Controllers
{
    [Authorize]
    [Route("api/player/heroes")]
    public class PlayerHeroesController : HRKControllerBase
    {
        private readonly IMediator _mediator;

        public PlayerHeroesController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("list")]
        public async Task<IActionResult> GetPlayerHeroes()
        {
            var response = await _mediator.Send(new GetPlayerHeroesQuery());
            return HrkOk(response);
        }

        [HttpGet("detail")]
        public async Task<IActionResult> GetPlayerHeroDetail([FromQuery] long heroId)
        {
            var response = await _mediator.Send(new GetPlayerHeroDetailQuery(heroId));
            return HrkOk(response);
        }
    }
}
