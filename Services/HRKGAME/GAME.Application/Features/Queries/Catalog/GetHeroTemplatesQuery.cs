using Core.Common.Entity.MyCompany.Shared.Responses;
using CoreEngine.CQRS;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;

namespace GAME.Application.Features.Queries.Catalog
{
    public record GetHeroTemplatesQuery : IQuery<BaseResponse<List<HeroTemplateDto>>>;

    public class GetHeroTemplatesQueryHandler : IQueryHandler<GetHeroTemplatesQuery, BaseResponse<List<HeroTemplateDto>>>
    {
        private readonly ICatalogService _catalogService;

        public GetHeroTemplatesQueryHandler(ICatalogService catalogService)
        {
            _catalogService = catalogService;
        }

        public async Task<BaseResponse<List<HeroTemplateDto>>> Handle(GetHeroTemplatesQuery request, CancellationToken cancellationToken)
        {
            var heroes = await _catalogService.GetHeroTemplatesAsync(cancellationToken);
            return BaseResponse<List<HeroTemplateDto>>.SuccessResponse(heroes, "Retrieved hero templates successfully.");
        }
    }
}
