using GAME.Application.Features.Queries.Formation;
using HRK.GAME.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRK.GAME.Controllers
{
    [Authorize]
    [Route("api/formation")]
    public class FormationController : HRKControllerBase
    {
        private readonly IMediator _mediator;

        public FormationController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("main")]
        public async Task<IActionResult> GetFormation()
        {
            var response = await _mediator.Send(new GetPlayerFormationQuery("Main Team"));
            return HrkOk(response);
        }
    }
}
