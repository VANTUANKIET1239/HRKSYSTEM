using GAME.Application.Features.Queries.Inventory;
using HRK.GAME.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRK.GAME.Controllers
{
    [Authorize]
    [Route("api/inventory")]
    public class InventoryController : HRKControllerBase
    {
        private readonly IMediator _mediator;

        public InventoryController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("items")]
        public async Task<IActionResult> GetInventory([FromQuery] string? categoryCode)
        {
            var response = await _mediator.Send(new GetPlayerInventoryQuery(categoryCode));
            return HrkOk(response);
        }

        [HttpGet("hero-equipment")]
        public async Task<IActionResult> GetHeroEquipment([FromQuery] long heroId)
        {
            var response = await _mediator.Send(new GetHeroEquipmentQuery(heroId));
            return HrkOk(response);
        }
    }
}
