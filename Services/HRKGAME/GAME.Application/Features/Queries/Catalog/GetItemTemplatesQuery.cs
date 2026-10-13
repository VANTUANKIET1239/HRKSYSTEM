using Core.Persistence.Repositories;
using GAME.Application.Common.Helpers;
using GAME.Application.Common.Mappings;
using GAME.Application.DTOs;
using GAME.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Core.Application.CQRS;
using Core.Common.Entity.MyCompany.Shared.Responses;

namespace GAME.Application.Features.Queries.Catalog;

public record GetItemTemplatesQuery(int? CategoryId = null, int? RarityId = null) : IQuery<BaseResponse<List<ItemTemplateDto>>>;

public sealed class GetItemTemplatesQueryHandler(IUnitOfWork unitOfWork) : IQueryHandler<GetItemTemplatesQuery, BaseResponse<List<ItemTemplateDto>>>
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    public async Task<BaseResponse<List<ItemTemplateDto>>> Handle(GetItemTemplatesQuery request, CancellationToken cancellationToken)
    {
        var items = await ExecuteAsync(request.CategoryId, request.RarityId, cancellationToken);
        return BaseResponse<List<ItemTemplateDto>>.SuccessResponse(items, "Retrieved item templates successfully.");
    }

    private async Task<List<ItemTemplateDto>> ExecuteAsync(int? categoryId, int? rarityId, CancellationToken cancellationToken = default)
    {
        var query = _unitOfWork.ReadOnlyRepository<HrkItemTemplate>().Query()
            .Include(i => i.Category)
            .Include(i => i.Rarity)
            .Include(i => i.Attributes).ThenInclude(a => a.AttributeType).AsQueryable();
        if (categoryId.HasValue)
        {
            query = query.Where(i => i.CategoryId == categoryId.Value);
        }

        if (rarityId.HasValue)
        {
            query = query.Where(i => i.RarityId == rarityId.Value);
        }

        var items = await query.ToListAsync(cancellationToken);
        return items.Select(i => new ItemTemplateDto { Id = i.Id, Code = i.Code ?? "", CategoryId = i.CategoryId, CategoryCode = i.Category?.Code ?? "", CategoryName = i.Category?.Name ?? "", RarityId = i.RarityId, RarityCode = i.Rarity?.Code ?? "", RarityName = i.Rarity?.Name ?? "", Name = i.Name, ImagePath = i.ImagePath, Icon = i.ImagePath ?? i.Icon ?? "", LevelReq = i.LevelReq, Description = i.Description, MetadataJson = i.MetadataJson, Attributes = i.Attributes?.Select(GameDtoMapper.MapItemAttribute).ToList() ?? new List<ItemAttributeDto>(), BaseStats = i.Attributes != null && i.Attributes.Count > 0 ? i.Attributes.ToDictionary(a => a.AttributeType?.Code ?? $"ATTR_{a.AttributeTypeId}", a => (object)a.Value) : GameJsonHelper.ParseJson(i.MetadataJson), IsStackable = i.IsStackable, MaxStackSize = i.MaxStackSize, SellPrice = i.SellPrice }).ToList();
    }
}
