using Core.Common.Entity.MyCompany.Shared.Responses;
using CoreEngine.CQRS;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;

namespace GAME.Application.Features.Queries.Metadata
{
    public record GetFactionsQuery : IQuery<BaseResponse<List<HeroFactionDto>>>;

    public class GetFactionsQueryHandler : IQueryHandler<GetFactionsQuery, BaseResponse<List<HeroFactionDto>>>
    {
        private readonly IMetadataService _metadataService;

        public GetFactionsQueryHandler(IMetadataService metadataService)
        {
            _metadataService = metadataService;
        }

        public async Task<BaseResponse<List<HeroFactionDto>>> Handle(GetFactionsQuery request, CancellationToken cancellationToken)
        {
            var factions = await _metadataService.GetFactionsAsync(cancellationToken);
            return BaseResponse<List<HeroFactionDto>>.SuccessResponse(factions, "Retrieved hero factions successfully.");
        }
    }
}
