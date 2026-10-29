using GAME.Application.DTOs;
using GAME.Application.Features.Commands.Inventory.EnhanceEquipment;
using GAME.Application.Features.Commands.Inventory.ExpandCapacity;
using GAME.Application.Features.Commands.Inventory.SellItems;
using GAME.Application.Features.Commands.Inventory.ToggleItemLock;
using GAME.Application.Features.Commands.Inventory.DowngradeEquipment;
using GAME.Application.Features.Queries.Inventory;
using GAME.Application.Features.Queries.Inventory.GetEnhancementConfigs;
using GAME.Application.Features.Queries.Inventory.GetEquipmentEnhancementPreview;
using GAME.Application.Features.Queries.Inventory.GetForgeEquipment;
using GAME.Application.Features.Queries.Inventory.GetEquipmentDowngradePreview;
using GAME.Application.Features.Queries.Inventory.GetPlayerEquipment;
using HRK.GAME.Common;
using Core.Application.CQRS;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRK.GAME.Controllers
{
    [Authorize]
    [Route("api/inventory")]
    public class InventoryController : HRKControllerBase
    {
        private readonly ICqrsDispatcher _dispatcher;

        public InventoryController(ICqrsDispatcher dispatcher)
        {
            _dispatcher = dispatcher;
        }

        [HttpGet("items")]
        public async Task<IActionResult> GetInventory([FromQuery] string? categoryCode)
        {
            var response = await _dispatcher.QueryAsync(new GetPlayerInventoryQuery(categoryCode),HttpContext.RequestAborted);
            return HrkOk(response);
        }

        [HttpGet("equipment")]
        public async Task<IActionResult> GetEquipment(
            [FromQuery] string? categoryCode,
            [FromQuery] bool includeEquipped = false)
        {
            var response = await _dispatcher.QueryAsync(new GetPlayerEquipmentQuery(categoryCode, includeEquipped),HttpContext.RequestAborted);
            return HrkOk(response);
        }

        [HttpGet("hero-equipment")]
        public async Task<IActionResult> GetHeroEquipment([FromQuery] long heroId)
        {
            var response = await _dispatcher.QueryAsync(new GetHeroEquipmentQuery(heroId),HttpContext.RequestAborted);
            return HrkOk(response);
        }

        [HttpPost("sell")]
        public async Task<IActionResult> SellItems([FromBody] SellItemsCommand command)
        {
            var response = await _dispatcher.CommandAsync(command,HttpContext.RequestAborted);
            return HrkOk(response);
        }

        [HttpPut("items/{id}/lock")]
        public async Task<IActionResult> ToggleLock([FromRoute] long id, [FromBody] ToggleLockRequest body)
        {
            var response = await _dispatcher.CommandAsync(new ToggleItemLockCommand(id, body.IsLocked),HttpContext.RequestAborted);
            return HrkOk(response);
        }

        [HttpPost("expand-capacity")]
        public async Task<IActionResult> ExpandCapacity([FromBody] ExpandInventoryCapacityCommand command)
        {
            var response = await _dispatcher.CommandAsync(command,HttpContext.RequestAborted);
            return HrkOk(response);
        }

        [HttpPost("enhance")]
        public async Task<IActionResult> EnhanceEquipment([FromBody] EnhanceEquipmentRequestDto request)
        {
            var response = await _dispatcher.CommandAsync(new EnhanceEquipmentCommand(request),HttpContext.RequestAborted);
            return HrkOk(response);
        }

        [HttpGet("enhancement/configs")]
        public async Task<IActionResult> GetEnhancementConfigs()
        {
            var response = await _dispatcher.QueryAsync(new GetEnhancementConfigsQuery(),HttpContext.RequestAborted);
            return HrkOk(response);
        }

        [HttpGet("forge-equipment")]
        public async Task<IActionResult> GetForgeEquipment()
        {
            var response = await _dispatcher.QueryAsync(new GetForgeEquipmentQuery(),HttpContext.RequestAborted);
            return HrkOk(response);
        }

        [HttpGet("enhancement/preview/{id}")]
        public async Task<IActionResult> GetEnhancementPreview([FromRoute] long id)
        {
            var response = await _dispatcher.QueryAsync(new GetEquipmentEnhancementPreviewQuery(id),HttpContext.RequestAborted);
            return HrkOk(response);
        }

        [HttpGet("enhancement/downgrade-preview/{inventoryItemId}")]
        public async Task<IActionResult> GetDowngradePreview([FromRoute] long inventoryItemId, [FromQuery] int targetLevel)
            => HrkOk(await _dispatcher.QueryAsync(new GetEquipmentDowngradePreviewQuery(inventoryItemId, targetLevel),HttpContext.RequestAborted));

        [HttpPost("enhancement/downgrade")]
        public async Task<IActionResult> Downgrade([FromBody] DowngradeEquipmentRequestDto request)
            => HrkOk(await _dispatcher.CommandAsync(new DowngradeEquipmentCommand(request),HttpContext.RequestAborted));
    }
}
