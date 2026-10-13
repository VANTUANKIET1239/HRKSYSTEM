using Core.Persistence.Repositories;
using GAME.Application.DTOs;
using GAME.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Core.Application.CQRS;
using Core.Common.Entity.MyCompany.Shared.Responses;

namespace GAME.Application.Features.Queries.Metadata;

public record GetItemCategoriesQuery : IQuery<BaseResponse<List<ItemCategoryDto>>>;

public sealed class GetItemCategoriesQueryHandler(IUnitOfWork unitOfWork) : IQueryHandler<GetItemCategoriesQuery, BaseResponse<List<ItemCategoryDto>>>
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    public async Task<BaseResponse<List<ItemCategoryDto>>> Handle(GetItemCategoriesQuery request, CancellationToken cancellationToken)
    {
        var categories = await ExecuteAsync(cancellationToken);
        return BaseResponse<List<ItemCategoryDto>>.SuccessResponse(categories, "Retrieved item categories successfully.");
    }

    private async Task<List<ItemCategoryDto>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.ReadOnlyRepository<HrkItemCategory>().Query()
            .OrderBy(c => c.DisplayOrder)
            .Select(c => new ItemCategoryDto { Id = c.Id, Code = c.Code, Name = c.Name, Icon = c.Icon, DisplayOrder = c.DisplayOrder, IsEquipment = c.IsEquipment, Description = c.Description })
            .ToListAsync(cancellationToken);
    }
}
