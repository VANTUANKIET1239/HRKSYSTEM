using Core.Common.CQRS;
using Core.Common.Entity.MyCompany.Shared.Responses;
using CoreEngine.CQRS;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;
using System.Threading;
using System.Threading.Tasks;

namespace GAME.Application.Features.Queries.Inventory.GetEnhancementConfigs
{
    public record GetEnhancementConfigsQuery() : IQuery<BaseResponse<EnhancementConfigResponseDto>>;

    public class GetEnhancementConfigsQueryHandler : IQueryHandler<GetEnhancementConfigsQuery, BaseResponse<EnhancementConfigResponseDto>>
    {
        private readonly IEquipmentEnhancementService _enhancementService;

        public GetEnhancementConfigsQueryHandler(IEquipmentEnhancementService enhancementService)
        {
            _enhancementService = enhancementService;
        }

        public async Task<BaseResponse<EnhancementConfigResponseDto>> Handle(GetEnhancementConfigsQuery request, CancellationToken cancellationToken)
        {
            var result = await _enhancementService.GetEnhancementConfigsAsync(cancellationToken);
            return BaseResponse<EnhancementConfigResponseDto>.SuccessResponse(result);
        }
    }
}
