using GAME.Application.DTOs;
using GAME.Application.Features.Commands.Inventory.EnhanceEquipment;
using GAME.Application.Features.Commands.Inventory.ExpandCapacity;
using GAME.Application.Features.Commands.Inventory.SellItems;
using GAME.Application.Features.Commands.Inventory.ToggleItemLock;
using GAME.Application.Features.Queries.Inventory;
using GAME.Application.Features.Queries.Inventory.GetEnhancementConfigs;
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

        [HttpPost("sell")]
        public async Task<IActionResult> SellItems([FromBody] SellItemsCommand command)
        {
            var response = await _mediator.Send(command);
            return HrkOk(response);
        }

        [HttpPut("items/{id}/lock")]
        public async Task<IActionResult> ToggleLock([FromRoute] long id, [FromBody] ToggleLockRequest body)
        {
            var response = await _mediator.Send(new ToggleItemLockCommand(id, body.IsLocked));
            return HrkOk(response);
        }

        [HttpPost("expand-capacity")]
        public async Task<IActionResult> ExpandCapacity([FromBody] ExpandInventoryCapacityCommand command)
        {
            var response = await _mediator.Send(command);
            return HrkOk(response);
        }

        [HttpPost("enhance")]
        public async Task<IActionResult> EnhanceEquipment([FromBody] EnhanceEquipmentRequestDto request)
        {
            var response = await _mediator.Send(new EnhanceEquipmentCommand(request));
            return HrkOk(response);
        }

        [HttpGet("enhancement/configs")]
        public async Task<IActionResult> GetEnhancementConfigs()
        {
            var response = await _mediator.Send(new GetEnhancementConfigsQuery());
            return HrkOk(response);
        }
    }
}
