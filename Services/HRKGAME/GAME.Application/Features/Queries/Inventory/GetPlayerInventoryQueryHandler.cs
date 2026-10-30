using GAME.Application.Templates;
using Core.Persistence.Repositories;
using GAME.Application.Common.Mappings;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;
using GAME.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Core.Application.CQRS;
using Core.Common.Entity.MyCompany.Shared.Responses;

namespace GAME.Application.Features.Queries.Inventory;

public sealed class GetPlayerInventoryQueryHandler(
    IUnitOfWork unitOfWork,
    IGamePlayerService gamePlayerService,
    IGameRequestContext actor, IGameTemplateCatalog? templates = null)
    : IQueryHandler<GetPlayerInventoryQuery, BaseResponse<List<InventoryItemDto>>>
{
    private readonly IGameTemplateCatalog _templates = templates ?? new DatabaseTemplateCatalog(unitOfWork);
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IGamePlayerService _gamePlayerService = gamePlayerService;
    private readonly IGameRequestContext _actor = actor;
    public async Task<BaseResponse<List<InventoryItemDto>>> Handle(GetPlayerInventoryQuery request, CancellationToken cancellationToken)
    {
        var userId = _actor.UserId;
        var items = await ExecuteAsync(userId, request.CategoryCode, cancellationToken);
        return BaseResponse<List<InventoryItemDto>>.SuccessResponse(items, "Retrieved player inventory successfully.");
    }

    private async Task<List<InventoryItemDto>> ExecuteAsync(string userId, string? categoryCode, CancellationToken cancellationToken = default)
    {
        var player = await _gamePlayerService.GetPlayerByUserIdAsync(userId, cancellationToken);
        if (player == null)
            return new List<InventoryItemDto>();
        var itemTemplates = (await _templates.GetAllAsync<HrkItemTemplate>(cancellationToken)).ToList();
        var allowedIds = itemTemplates.Where(t => string.IsNullOrWhiteSpace(categoryCode) ||
            categoryCode.Equals("all", StringComparison.OrdinalIgnoreCase) || t.Category.Code == categoryCode)
            .Select(t => t.Id).ToArray();
        var query = _unitOfWork.ReadOnlyRepository<HrkPlayerInventory>().Query()
            .Where(inv => inv.PlayerId == player.Id && inv.IsActive)
            .Where(inv => allowedIds.Contains(inv.ItemTemplateId))
            .Include(inv => inv.Attributes)
            .Include(inv => inv.EquippedHero)
            .AsSplitQuery().AsQueryable();
        var items = await query.ToListAsync(cancellationToken);
        await TemplateHydration.InventoryAsync(_templates, items, cancellationToken);
        await TemplateHydration.HeroesAsync(_templates, items.Select(i => i.EquippedHero).OfType<HrkPlayerHero>(), cancellationToken);
        var result = items.OrderByDescending(i => i.ItemTemplate.RarityId).Select(inv => GameDtoMapper.MapInventoryItem(inv)!).ToList();
        var templateIds = items.Select(x => x.ItemTemplateId).Distinct().ToList();
        var stonePortraits = (
            from stone in (await _templates.GetAllAsync<HrkHeroStoneConfig>(cancellationToken))
            join hero in (await _templates.GetAllAsync<HrkHeroTemplate>(cancellationToken)) on stone.HeroTemplateId equals hero.Id
            where stone.IsActive && templateIds.Contains(stone.ItemTemplateId)
            select new
            {
                stone.ItemTemplateId,
                hero.Avatar
            }

        ).ToDictionary(x => x.ItemTemplateId, x => x.Avatar);
        foreach (var item in result)
        {
            if (stonePortraits.TryGetValue(item.ItemTemplateId, out var portrait))
            {
                item.HeroStonePortrait = portrait;
            }
        }

        return result;
    }
}
