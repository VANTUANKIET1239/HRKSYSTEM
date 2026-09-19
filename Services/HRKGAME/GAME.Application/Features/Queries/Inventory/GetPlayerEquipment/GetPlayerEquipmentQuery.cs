using Core.Common.CQRS;
using Core.Common.Entity.MyCompany.Shared.Responses;
using CoreEngine.CQRS;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GAME.Application.Features.Queries.Inventory.GetPlayerEquipment
{
    public record GetPlayerEquipmentQuery(string? CategoryCode = null, bool IncludeEquipped = false) : IQuery<BaseResponse<List<InventoryItemDto>>>;

    public class GetPlayerEquipmentQueryHandler : HRKBaseQuery, IQueryHandler<GetPlayerEquipmentQuery, BaseResponse<List<InventoryItemDto>>>
    {
        private readonly IInventoryService _inventoryService;

        public GetPlayerEquipmentQueryHandler(IInventoryService inventoryService, IHttpContextAccessor httpContextAccessor)
            : base(httpContextAccessor)
        {
            _inventoryService = inventoryService;
        }

        public async Task<BaseResponse<List<InventoryItemDto>>> Handle(GetPlayerEquipmentQuery request, CancellationToken cancellationToken)
        {
            var userId = GetUserId();
            var items = await _inventoryService.GetPlayerEquipmentAsync(userId, request.CategoryCode, request.IncludeEquipped, cancellationToken);
            return BaseResponse<List<InventoryItemDto>>.SuccessResponse(items, "Retrieved player equipment successfully.");
        }
    }
}
