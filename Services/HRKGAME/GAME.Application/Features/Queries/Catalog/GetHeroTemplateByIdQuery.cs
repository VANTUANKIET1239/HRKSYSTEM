using Core.Common.Entity.MyCompany.Shared.Responses;
using CoreEngine.CQRS;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;

namespace GAME.Application.Features.Queries.Catalog
{
    public record GetHeroTemplateByIdQuery(int Id) : IQuery<BaseResponse<HeroTemplateDto>>;

    public class GetHeroTemplateByIdQueryHandler : IQueryHandler<GetHeroTemplateByIdQuery, BaseResponse<HeroTemplateDto>>
    {
        private readonly ICatalogService _catalogService;

        public GetHeroTemplateByIdQueryHandler(ICatalogService catalogService)
        {
            _catalogService = catalogService;
        }

        public async Task<BaseResponse<HeroTemplateDto>> Handle(GetHeroTemplateByIdQuery request, CancellationToken cancellationToken)
        {
            var hero = await _catalogService.GetHeroTemplateByIdAsync(request.Id, cancellationToken);
            if (hero == null)
            {
                return BaseResponse<HeroTemplateDto>.FailResponse("Hero template not found.", statusCode: 404);
            }
            return BaseResponse<HeroTemplateDto>.SuccessResponse(hero, "Retrieved hero template detail successfully.");
        }
    }
}
