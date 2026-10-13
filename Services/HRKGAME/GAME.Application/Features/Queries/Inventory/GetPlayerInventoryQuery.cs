using Core.Persistence.Repositories;
using GAME.Application.Common.Mappings;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;
using GAME.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Core.Application.CQRS;
using Core.Common.Entity.MyCompany.Shared.Responses;

namespace GAME.Application.Features.Queries.Inventory;

public record GetPlayerInventoryQuery(string? CategoryCode = null) : IQuery<BaseResponse<List<InventoryItemDto>>>;

public sealed class GetPlayerInventoryQueryHandler(
    IUnitOfWork unitOfWork,
    IGamePlayerService gamePlayerService,
    IGameRequestContext actor)
    : IQueryHandler<GetPlayerInventoryQuery, BaseResponse<List<InventoryItemDto>>>
{
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
        var query = _unitOfWork.ReadOnlyRepository<HrkPlayerInventory>().Query()
            .Where(inv => inv.PlayerId == player.Id && inv.IsActive)
            .Include(inv => inv.Attributes).ThenInclude(a => a.AttributeType)
            .Include(inv => inv.ItemTemplate).ThenInclude(it => it.Category)
            .Include(inv => inv.ItemTemplate).ThenInclude(it => it.Rarity)
            .Include(inv => inv.ItemTemplate).ThenInclude(it => it.Attributes).ThenInclude(a => a.AttributeType)
            .Include(inv => inv.EquippedHero).ThenInclude(hero => hero!.HeroTemplate)
            .OrderByDescending(x => x.ItemTemplate.RarityId)
            .AsSplitQuery().AsQueryable();
        if (!string.IsNullOrWhiteSpace(categoryCode) && !categoryCode.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(inv => inv.ItemTemplate.Category.Code == categoryCode);
        }

        var items = await query.ToListAsync(cancellationToken);
        var result = items.Select(inv => GameDtoMapper.MapInventoryItem(inv)!).ToList();
        var templateIds = items.Select(x => x.ItemTemplateId).Distinct().ToList();
        var stonePortraits = await (
            from stone in _unitOfWork.ReadOnlyRepository<HrkHeroStoneConfig>().Query()
            join hero in _unitOfWork.ReadOnlyRepository<HrkHeroTemplate>().Query() on stone.HeroTemplateId equals hero.Id
            where stone.IsActive && templateIds.Contains(stone.ItemTemplateId)
            select new
            {
                stone.ItemTemplateId,
                hero.Avatar
            }

        ).ToDictionaryAsync(x => x.ItemTemplateId, x => x.Avatar, cancellationToken);
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
