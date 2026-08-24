using Core.Common.CQRS;
using Core.Common.Entity.MyCompany.Shared.Responses;
using CoreEngine.CQRS;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;
using Microsoft.AspNetCore.Http;

namespace GAME.Application.Features.Queries.Inventory
{
    public record GetHeroEquipmentQuery(long HeroId) : IQuery<BaseResponse<HeroEquipmentDto>>;

    public class GetHeroEquipmentQueryHandler : HRKBaseQuery, IQueryHandler<GetHeroEquipmentQuery, BaseResponse<HeroEquipmentDto>>
    {
        private readonly IInventoryService _inventoryService;

        public GetHeroEquipmentQueryHandler(IInventoryService inventoryService, IHttpContextAccessor httpContextAccessor)
            : base(httpContextAccessor)
        {
            _inventoryService = inventoryService;
        }

        public async Task<BaseResponse<HeroEquipmentDto>> Handle(GetHeroEquipmentQuery request, CancellationToken cancellationToken)
        {
            var userId = GetUserId();
            var eq = await _inventoryService.GetHeroEquipmentAsync(userId, request.HeroId, cancellationToken);
            return BaseResponse<HeroEquipmentDto>.SuccessResponse(eq, "Retrieved hero equipment successfully.");
        }
    }
}
