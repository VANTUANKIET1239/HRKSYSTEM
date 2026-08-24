using Core.Common.CQRS;
using Core.Common.Entity.MyCompany.Shared.Responses;
using CoreEngine.CQRS;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;
using Microsoft.AspNetCore.Http;

namespace GAME.Application.Features.Queries.Inventory
{
    public record GetPlayerInventoryQuery(string? CategoryCode = null) : IQuery<BaseResponse<List<InventoryItemDto>>>;

    public class GetPlayerInventoryQueryHandler : HRKBaseQuery, IQueryHandler<GetPlayerInventoryQuery, BaseResponse<List<InventoryItemDto>>>
    {
        private readonly IInventoryService _inventoryService;

        public GetPlayerInventoryQueryHandler(IInventoryService inventoryService, IHttpContextAccessor httpContextAccessor)
            : base(httpContextAccessor)
        {
            _inventoryService = inventoryService;
        }

        public async Task<BaseResponse<List<InventoryItemDto>>> Handle(GetPlayerInventoryQuery request, CancellationToken cancellationToken)
        {
            var userId = GetUserId();
            var items = await _inventoryService.GetPlayerInventoryAsync(userId, request.CategoryCode, cancellationToken);
            return BaseResponse<List<InventoryItemDto>>.SuccessResponse(items, "Retrieved player inventory successfully.");
        }
    }
}
