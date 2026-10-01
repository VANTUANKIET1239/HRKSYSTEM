using GAME.Application.DTOs;
using GAME.Application.Features.Commands.PlayerHeroes;
using GAME.Application.Features.Commands.PlayerHeroes.EquipHeroItem;
using GAME.Application.Features.Commands.PlayerHeroes.UnequipHeroItem;
using GAME.Application.Features.Commands.PlayerHeroes.UnequipAllHeroItems;
using GAME.Application.Features.Commands.PlayerHeroes.SwapHeroEquipment;
using GAME.Application.Features.Queries.PlayerHeroes;
using GAME.Application.Interfaces;
using HRK.GAME.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace HRK.GAME.Controllers
{
    [Authorize]
    [Route("api/player/heroes")]
    public class PlayerHeroesController : HRKControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IHeroUpgradeService _heroUpgradeService;
        private readonly IHeroStarUpgradeService _heroStarUpgradeService;

        public PlayerHeroesController(IMediator mediator, IHeroUpgradeService heroUpgradeService, IHeroStarUpgradeService heroStarUpgradeService)
        {
            _mediator = mediator;
            _heroUpgradeService = heroUpgradeService;
            _heroStarUpgradeService = heroStarUpgradeService;
        }

        [HttpGet("{heroId}/star-upgrade-preview")]
        public async Task<IActionResult> GetStarUpgradePreview([FromRoute] long heroId, CancellationToken cancellationToken)
        {
            try { return HrkOk(Core.Common.Entity.MyCompany.Shared.Responses.BaseResponse<HeroStarUpgradePreviewDto>.SuccessResponse(await _heroStarUpgradeService.PreviewAsync(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException(), heroId, cancellationToken))); }
            catch (Exception ex) when (ex is KeyNotFoundException or InvalidOperationException) { return HrkOk(Core.Common.Entity.MyCompany.Shared.Responses.BaseResponse<object>.FailResponse(ex.Message, statusCode: 400)); }
        }

        [HttpPost("{heroId}/star-upgrade")]
        public async Task<IActionResult> StarUpgrade([FromRoute] long heroId, [FromBody] HeroStarUpgradeRequestDto request, CancellationToken cancellationToken)
        {
            try { return HrkOk(Core.Common.Entity.MyCompany.Shared.Responses.BaseResponse<HeroStarUpgradePreviewDto>.SuccessResponse(await _heroStarUpgradeService.UpgradeAsync(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException(), heroId, request.RequestId, cancellationToken, request.MaterialType), "Tăng sao thành công.")); }
            catch (Exception ex) when (ex is KeyNotFoundException or InvalidOperationException) { return HrkOk(Core.Common.Entity.MyCompany.Shared.Responses.BaseResponse<object>.FailResponse(ex.Message, statusCode: 400)); }
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

        [HttpGet("{heroId}/upgrade-preview")]
        public async Task<IActionResult> GetUpgradePreview([FromRoute] long heroId, CancellationToken cancellationToken)
        {
            try
            {
                var preview = await _heroUpgradeService.GetPreviewAsync(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                    ?? throw new UnauthorizedAccessException(), heroId, cancellationToken);
                return HrkOk(Core.Common.Entity.MyCompany.Shared.Responses.BaseResponse<global::GAME.Application.DTOs.HeroUpgradePreviewDto>.SuccessResponse(preview));
            }
            catch (KeyNotFoundException ex) { return HrkOk(Core.Common.Entity.MyCompany.Shared.Responses.BaseResponse<object>.FailResponse(ex.Message, statusCode: 404)); }
            catch (InvalidOperationException ex) { return HrkOk(Core.Common.Entity.MyCompany.Shared.Responses.BaseResponse<object>.FailResponse(ex.Message, statusCode: 400)); }
        }

        [HttpPost("{heroId}/upgrade")]
        public async Task<IActionResult> Upgrade([FromRoute] long heroId, [FromBody] global::GAME.Application.DTOs.UpgradeHeroRequestDto request, CancellationToken cancellationToken)
        {
            try
            {
                var detail = await _heroUpgradeService.UpgradeAsync(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                    ?? throw new UnauthorizedAccessException(), heroId, request?.Levels ?? 1, cancellationToken);
                return HrkOk(Core.Common.Entity.MyCompany.Shared.Responses.BaseResponse<global::GAME.Application.DTOs.PlayerHeroDetailDto>.SuccessResponse(detail, "Nang cap vo tuong thanh cong."));
            }
            catch (KeyNotFoundException ex) { return HrkOk(Core.Common.Entity.MyCompany.Shared.Responses.BaseResponse<object>.FailResponse(ex.Message, statusCode: 404)); }
            catch (InvalidOperationException ex) { return HrkOk(Core.Common.Entity.MyCompany.Shared.Responses.BaseResponse<object>.FailResponse(ex.Message, statusCode: 400)); }
        }

        [HttpPatch("{heroId}/flags")]
        public async Task<IActionResult> UpdateFlags([FromRoute] long heroId, [FromBody] UpdatePlayerHeroFlagsRequest request)
        {
            var response = await _mediator.Send(new UpdatePlayerHeroFlagsCommand(heroId, request));
            return HrkOk(response);
        }

        [HttpPost("{heroId}/equipment")]
        public async Task<IActionResult> EquipHeroItem([FromRoute] long heroId, [FromBody] EquipHeroItemRequestDto request)
        {
            var response = await _mediator.Send(new EquipHeroItemCommand(heroId, request));
            return HrkOk(response);
        }

        [HttpDelete("{heroId}/equipment/{slotCode}")]
        public async Task<IActionResult> UnequipHeroItem([FromRoute] long heroId, [FromRoute] string slotCode)
        {
            var response = await _mediator.Send(new UnequipHeroItemCommand(heroId, slotCode));
            return HrkOk(response);
        }

        [HttpDelete("{heroId}/equipment")]
        public async Task<IActionResult> UnequipAllHeroItems([FromRoute] long heroId)
        {
            var response = await _mediator.Send(new UnequipAllHeroItemsCommand(heroId));
            return HrkOk(response);
        }

        [HttpPost("{heroId}/equipment/swap")]
        public async Task<IActionResult> SwapHeroEquipment([FromRoute] long heroId, [FromBody] SwapHeroEquipmentRequestDto request)
        {
            var response = await _mediator.Send(new SwapHeroEquipmentCommand(heroId, request));
            return HrkOk(response);
        }
    }
}
