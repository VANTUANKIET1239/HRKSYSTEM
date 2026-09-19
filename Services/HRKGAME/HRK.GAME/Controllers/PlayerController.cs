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

        [HttpGet("me")]
        public async Task<IActionResult> GetGameInfo()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();
            var service = HttpContext.RequestServices.GetRequiredService<global::GAME.Application.Interfaces.IGamePlayerService>();
            var data = await service.GetPlayerGameInfoAsync(userId);
            return HrkOk(Core.Common.Entity.MyCompany.Shared.Responses.BaseResponse<global::GAME.Application.DTOs.PlayerGameInfoDto?>.SuccessResponse(data));
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
