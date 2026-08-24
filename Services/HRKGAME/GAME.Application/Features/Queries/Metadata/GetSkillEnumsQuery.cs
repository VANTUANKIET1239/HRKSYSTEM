using Core.Common.Entity.MyCompany.Shared.Responses;
using CoreEngine.CQRS;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;

namespace GAME.Application.Features.Queries.Metadata
{
    public record GetSkillEnumsQuery : IQuery<BaseResponse<SkillEnumsDto>>;

    public class GetSkillEnumsQueryHandler : IQueryHandler<GetSkillEnumsQuery, BaseResponse<SkillEnumsDto>>
    {
        private readonly IMetadataService _metadataService;

        public GetSkillEnumsQueryHandler(IMetadataService metadataService)
        {
            _metadataService = metadataService;
        }

        public async Task<BaseResponse<SkillEnumsDto>> Handle(GetSkillEnumsQuery request, CancellationToken cancellationToken)
        {
            var result = await _metadataService.GetSkillEnumsAsync(cancellationToken);
            return BaseResponse<SkillEnumsDto>.SuccessResponse(result, "Retrieved skill enums successfully.");
        }
    }
}
