using Core.Common.Entity.MyCompany.Shared.Responses;
using CoreEngine.CQRS;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;

namespace GAME.Application.Features.Queries.Metadata
{
    public record GetItemCategoriesQuery : IQuery<BaseResponse<List<ItemCategoryDto>>>;

    public class GetItemCategoriesQueryHandler : IQueryHandler<GetItemCategoriesQuery, BaseResponse<List<ItemCategoryDto>>>
    {
        private readonly IMetadataService _metadataService;

        public GetItemCategoriesQueryHandler(IMetadataService metadataService)
        {
            _metadataService = metadataService;
        }

        public async Task<BaseResponse<List<ItemCategoryDto>>> Handle(GetItemCategoriesQuery request, CancellationToken cancellationToken)
        {
            var categories = await _metadataService.GetItemCategoriesAsync(cancellationToken);
            return BaseResponse<List<ItemCategoryDto>>.SuccessResponse(categories, "Retrieved item categories successfully.");
        }
    }
}
