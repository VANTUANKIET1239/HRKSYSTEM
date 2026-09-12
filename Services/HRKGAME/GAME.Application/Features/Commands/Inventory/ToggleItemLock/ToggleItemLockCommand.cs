using Core.Common.CQRS;
using Core.Common.Entity.MyCompany.Shared.Responses;
using CoreEngine.CQRS;
using GAME.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace GAME.Application.Features.Commands.Inventory.ToggleItemLock
{
    public record ToggleItemLockCommand(long InventoryItemId, bool IsLocked) : ICommand<BaseResponse<bool>>;

    public class ToggleItemLockCommandHandler : HRKBaseCommand, ICommandHandler<ToggleItemLockCommand, BaseResponse<bool>>
    {
        private readonly IInventoryService _inventoryService;

        public ToggleItemLockCommandHandler(IInventoryService inventoryService, IHttpContextAccessor httpContextAccessor)
            : base(httpContextAccessor)
        {
            _inventoryService = inventoryService;
        }

        public async Task<BaseResponse<bool>> Handle(ToggleItemLockCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var userId = GetUserId();
                var result = await _inventoryService.ToggleItemLockAsync(userId, request.InventoryItemId, request.IsLocked, cancellationToken);
                return BaseResponse<bool>.SuccessResponse(result, result ? "Đã khóa vật phẩm thành công." : "Đã mở khóa vật phẩm thành công.");
            }
            catch (KeyNotFoundException ex)
            {
                return BaseResponse<bool>.FailResponse(ex.Message, statusCode: 404);
            }
            catch (Exception ex)
            {
                return BaseResponse<bool>.FailResponse(ex.Message, statusCode: 400);
            }
        }
    }
}
