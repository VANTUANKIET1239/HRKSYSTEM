using Core.Common.Entity.MyCompany.Shared.Responses;
using CoreEngine.CQRS;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;

namespace GAME.Application.Features.Queries.Metadata
{
    public record GetClassesQuery : IQuery<BaseResponse<List<HeroClassDto>>>;

    public class GetClassesQueryHandler : IQueryHandler<GetClassesQuery, BaseResponse<List<HeroClassDto>>>
    {
        private readonly IMetadataService _metadataService;

        public GetClassesQueryHandler(IMetadataService metadataService)
        {
            _metadataService = metadataService;
        }

        public async Task<BaseResponse<List<HeroClassDto>>> Handle(GetClassesQuery request, CancellationToken cancellationToken)
        {
            var classes = await _metadataService.GetClassesAsync(cancellationToken);
            return BaseResponse<List<HeroClassDto>>.SuccessResponse(classes, "Retrieved hero classes successfully.");
        }
    }
}
