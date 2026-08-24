using Core.Common.Entity.MyCompany.Shared.Responses;
using CoreEngine.CQRS;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;

namespace GAME.Application.Features.Queries.Catalog
{
    public record GetItemTemplatesQuery(int? CategoryId = null, int? RarityId = null) : IQuery<BaseResponse<List<ItemTemplateDto>>>;

    public class GetItemTemplatesQueryHandler : IQueryHandler<GetItemTemplatesQuery, BaseResponse<List<ItemTemplateDto>>>
    {
        private readonly ICatalogService _catalogService;

        public GetItemTemplatesQueryHandler(ICatalogService catalogService)
        {
            _catalogService = catalogService;
        }

        public async Task<BaseResponse<List<ItemTemplateDto>>> Handle(GetItemTemplatesQuery request, CancellationToken cancellationToken)
        {
            var items = await _catalogService.GetItemTemplatesAsync(request.CategoryId, request.RarityId, cancellationToken);
            return BaseResponse<List<ItemTemplateDto>>.SuccessResponse(items, "Retrieved item templates successfully.");
        }
    }
}
