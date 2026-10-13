using Core.Persistence.Repositories;
using GAME.Application.Common.Mappings;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;
using GAME.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Core.Application.CQRS;
using Core.Common.Entity.MyCompany.Shared.Responses;

namespace GAME.Application.Features.Queries.Inventory.GetPlayerEquipment;

public record GetPlayerEquipmentQuery(string? CategoryCode = null, bool IncludeEquipped = false) : IQuery<BaseResponse<List<InventoryItemDto>>>;

public sealed class GetPlayerEquipmentQueryHandler(
    IUnitOfWork unitOfWork,
    IGamePlayerService gamePlayerService,
    IGameRequestContext actor)
    : IQueryHandler<GetPlayerEquipmentQuery, BaseResponse<List<InventoryItemDto>>>
{
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
        var query = _unitOfWork.ReadOnlyRepository<HrkPlayerInventory>().Query()
            .Where(inv => inv.PlayerId == player.Id && inv.IsActive && inv.ItemTemplate.Category.IsEquipment)
            .Include(inv => inv.Attributes).ThenInclude(a => a.AttributeType)
            .Include(inv => inv.ItemTemplate).ThenInclude(it => it.Category)
            .Include(inv => inv.ItemTemplate).ThenInclude(it => it.Rarity)
            .Include(inv => inv.ItemTemplate).ThenInclude(it => it.Attributes).ThenInclude(a => a.AttributeType)
            .Include(inv => inv.EquippedHero).ThenInclude(hero => hero!.HeroTemplate)
            .AsSplitQuery().AsQueryable();
        if (!includeEquipped)
        {
            query = query.Where(inv => !inv.IsEquipped);
        }

        if (!string.IsNullOrWhiteSpace(categoryCode) && !categoryCode.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            var upperCategoryCode = categoryCode.ToUpper();
            query = query.Where(inv => inv.ItemTemplate.Category.Code.ToUpper() == upperCategoryCode);
        }

        query = query.OrderByDescending(x => x.ItemTemplate.RarityId).ThenByDescending(x => x.Enhancement).ThenBy(x => x.ItemTemplate.Name);
        var items = await query.ToListAsync(cancellationToken);
        return items.Select(inv => GameDtoMapper.MapInventoryItem(inv)!).ToList();
    }
}
