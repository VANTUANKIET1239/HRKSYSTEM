using Core.Common.Entity.MyCompany.Shared.Responses;
using CoreEngine.CQRS;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;

namespace GAME.Application.Features.Queries.Catalog
{
    public record GetSkillTemplatesQuery : IQuery<BaseResponse<List<SkillTemplateDto>>>;

    public class GetSkillTemplatesQueryHandler : IQueryHandler<GetSkillTemplatesQuery, BaseResponse<List<SkillTemplateDto>>>
    {
        private readonly ICatalogService _catalogService;

        public GetSkillTemplatesQueryHandler(ICatalogService catalogService)
        {
            _catalogService = catalogService;
        }

        public async Task<BaseResponse<List<SkillTemplateDto>>> Handle(GetSkillTemplatesQuery request, CancellationToken cancellationToken)
        {
            var skills = await _catalogService.GetSkillTemplatesAsync(cancellationToken);
            return BaseResponse<List<SkillTemplateDto>>.SuccessResponse(skills, "Retrieved skill templates successfully.");
        }
    }
}
