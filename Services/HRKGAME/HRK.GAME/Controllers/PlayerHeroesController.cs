using GAME.Application.DTOs;
using GAME.Application.Features.Commands.PlayerHeroes;
using GAME.Application.Features.Commands.PlayerHeroes.EquipHeroItem;
using GAME.Application.Features.Commands.PlayerHeroes.UnequipHeroItem;
using GAME.Application.Features.Commands.PlayerHeroes.UnequipAllHeroItems;
using GAME.Application.Features.Commands.PlayerHeroes.SwapHeroEquipment;
using GAME.Application.Features.Queries.PlayerHeroes;
using HRK.GAME.Common;
using Core.Application.CQRS;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using GAME.Application.Features.Queries.HeroStarUpgrade;
using GAME.Application.Features.Commands.HeroStarUpgrade;
using GAME.Application.Features.Queries.HeroUpgrade;
using GAME.Application.Features.Commands.HeroUpgrade;

namespace HRK.GAME.Controllers;

[Authorize]
[Route("api/player/heroes")]
public sealed class PlayerHeroesController : HRKControllerBase
{
    private readonly ICqrsDispatcher _dispatcher;
    private string UserId => RequireUserId();

    public PlayerHeroesController(ICqrsDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    [HttpGet("{heroId}/star-upgrade-preview")]
    public async Task<IActionResult> GetStarUpgradePreview([FromRoute] long heroId, CancellationToken cancellationToken)
    {
        return HrkOk(await _dispatcher.QueryAsync(new GetHeroStarUpgradePreviewQuery(UserId, heroId), cancellationToken));
    }

    [HttpPost("{heroId}/star-upgrade")]
    public async Task<IActionResult> StarUpgrade([FromRoute] long heroId, [FromBody] HeroStarUpgradeRequestDto request, CancellationToken cancellationToken)
    {
        return HrkOk(await _dispatcher.CommandAsync(new UpgradeHeroStarCommand(UserId, heroId, request.RequestId, request.MaterialType), cancellationToken));
    }

    [HttpGet("list")]
    public async Task<IActionResult> GetPlayerHeroes()
    {
        var response = await _dispatcher.QueryAsync(new GetPlayerHeroesQuery(), HttpContext.RequestAborted);
        return HrkOk(response);
    }

    [HttpGet("detail")]
    public async Task<IActionResult> GetPlayerHeroDetail([FromQuery] long heroId)
    {
        var response = await _dispatcher.QueryAsync(new GetPlayerHeroDetailQuery(heroId), HttpContext.RequestAborted);
        return HrkOk(response);
    }

    [HttpGet("{heroId}/upgrade-preview")]
    public async Task<IActionResult> GetUpgradePreview([FromRoute] long heroId, CancellationToken cancellationToken)
    {
        return HrkOk(await _dispatcher.QueryAsync(new GetHeroUpgradePreviewQuery(UserId, heroId), cancellationToken));
    }

    [HttpPost("{heroId}/upgrade")]
    public async Task<IActionResult> Upgrade([FromRoute] long heroId, [FromBody] global::GAME.Application.DTOs.UpgradeHeroRequestDto request, CancellationToken cancellationToken)
    {
        return HrkOk(await _dispatcher.CommandAsync(new UpgradeHeroCommand(UserId, heroId, request?.Levels ?? 1), cancellationToken));
    }

    [HttpPatch("{heroId}/flags")]
    public async Task<IActionResult> UpdateFlags([FromRoute] long heroId, [FromBody] UpdatePlayerHeroFlagsRequest request)
    {
        var response = await _dispatcher.CommandAsync(new UpdatePlayerHeroFlagsCommand(heroId, request), HttpContext.RequestAborted);
        return HrkOk(response);
    }

    [HttpPost("{heroId}/equipment")]
    public async Task<IActionResult> EquipHeroItem([FromRoute] long heroId, [FromBody] EquipHeroItemRequestDto request)
    {
        var response = await _dispatcher.CommandAsync(new EquipHeroItemCommand(heroId, request), HttpContext.RequestAborted);
        return HrkOk(response);
    }

    [HttpDelete("{heroId}/equipment/{slotCode}")]
    public async Task<IActionResult> UnequipHeroItem([FromRoute] long heroId, [FromRoute] string slotCode)
    {
        var response = await _dispatcher.CommandAsync(new UnequipHeroItemCommand(heroId, slotCode), HttpContext.RequestAborted);
        return HrkOk(response);
    }

    [HttpDelete("{heroId}/equipment")]
    public async Task<IActionResult> UnequipAllHeroItems([FromRoute] long heroId)
    {
        var response = await _dispatcher.CommandAsync(new UnequipAllHeroItemsCommand(heroId), HttpContext.RequestAborted);
        return HrkOk(response);
    }

    [HttpPost("{heroId}/equipment/swap")]
    public async Task<IActionResult> SwapHeroEquipment([FromRoute] long heroId, [FromBody] SwapHeroEquipmentRequestDto request)
    {
        var response = await _dispatcher.CommandAsync(new SwapHeroEquipmentCommand(heroId, request), HttpContext.RequestAborted);
        return HrkOk(response);
    }
}
