using Core.Common.Entity.MyCompany.Shared.Responses;
using CoreEngine.CQRS;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;

namespace GAME.Application.Features.Queries.Metadata
{
    public record GetRaritiesQuery : IQuery<BaseResponse<List<RarityDto>>>;

    public class GetRaritiesQueryHandler : IQueryHandler<GetRaritiesQuery, BaseResponse<List<RarityDto>>>
    {
        private readonly IMetadataService _metadataService;

        public GetRaritiesQueryHandler(IMetadataService metadataService)
        {
            _metadataService = metadataService;
        }

        public async Task<BaseResponse<List<RarityDto>>> Handle(GetRaritiesQuery request, CancellationToken cancellationToken)
        {
            var rarities = await _metadataService.GetRaritiesAsync(cancellationToken);
            return BaseResponse<List<RarityDto>>.SuccessResponse(rarities, "Retrieved rarities successfully.");
        }
    }
}
