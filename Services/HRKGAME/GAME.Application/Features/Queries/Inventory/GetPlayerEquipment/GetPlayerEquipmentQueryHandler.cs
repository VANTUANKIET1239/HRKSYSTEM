using Core.Persistence.Repositories;
using GAME.Application.Templates;
using GAME.Application.Common.Mappings;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;
using GAME.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Core.Application.CQRS;
using Core.Common.Entity.MyCompany.Shared.Responses;

namespace GAME.Application.Features.Queries.Inventory.GetPlayerEquipment;

public sealed class GetPlayerEquipmentQueryHandler(
    IUnitOfWork unitOfWork,
    IGamePlayerService gamePlayerService,
    IGameRequestContext actor, IGameTemplateCatalog? templates = null)
    : IQueryHandler<GetPlayerEquipmentQuery, BaseResponse<List<InventoryItemDto>>>
{
    private readonly IGameTemplateCatalog _templates = templates ?? new DatabaseTemplateCatalog(unitOfWork);
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IGamePlayerService _gamePlayerService = gamePlayerService;
    private readonly IGameRequestContext _actor = actor;
    public async Task<BaseResponse<List<InventoryItemDto>>> Handle(GetPlayerEquipmentQuery request, CancellationToken cancellationToken)
    {
        var userId = _actor.UserId;
        var items = await ExecuteAsync(userId, request.CategoryCode, request.IncludeEquipped, cancellationToken);
        return BaseResponse<List<InventoryItemDto>>.SuccessResponse(items, "Retrieved player equipment successfully.");
    }

    private async Task<List<InventoryItemDto>> ExecuteAsync(string userId, string? categoryCode, bool includeEquipped = false, CancellationToken cancellationToken = default)
    {
        var player = await _gamePlayerService.GetPlayerByUserIdAsync(userId, cancellationToken);
        if (player == null)
            return new List<InventoryItemDto>();
        var itemTemplates = (await _templates.GetAllAsync<HrkItemTemplate>(cancellationToken)).ToList();
        var allowedIds = itemTemplates.Where(t => t.Category.IsEquipment &&
            (string.IsNullOrWhiteSpace(categoryCode) || categoryCode.Equals("all", StringComparison.OrdinalIgnoreCase) ||
             t.Category.Code.Equals(categoryCode, StringComparison.OrdinalIgnoreCase))).Select(t => t.Id).ToArray();
        var query = _unitOfWork.ReadOnlyRepository<HrkPlayerInventory>().Query()
            .Where(inv => inv.PlayerId == player.Id && inv.IsActive && allowedIds.Contains(inv.ItemTemplateId))
            .Include(inv => inv.Attributes)
            .Include(inv => inv.EquippedHero)
            .AsSplitQuery().AsQueryable();
        if (!includeEquipped)
        {
            query = query.Where(inv => !inv.IsEquipped);
        }

        var items = await query.ToListAsync(cancellationToken);
        await TemplateHydration.InventoryAsync(_templates, items, cancellationToken);
        await TemplateHydration.HeroesAsync(_templates, items.Select(i => i.EquippedHero).OfType<HrkPlayerHero>(), cancellationToken);
        return items.OrderByDescending(x => x.ItemTemplate.RarityId).ThenByDescending(x => x.Enhancement)
            .ThenBy(x => x.ItemTemplate.Name).Select(inv => GameDtoMapper.MapInventoryItem(inv)!).ToList();
    }
}
