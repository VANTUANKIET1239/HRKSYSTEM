using Core.Common.CQRS;
using Core.Common.Entity.MyCompany.Shared.Responses;
using CoreEngine.CQRS;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace GAME.Application.Features.Commands.Inventory.ExpandCapacity
{
    public record ExpandInventoryCapacityCommand(int SlotsToAdd) : ICommand<BaseResponse<PlayerWalletDto>>;

    public class ExpandInventoryCapacityCommandHandler : HRKBaseCommand, ICommandHandler<ExpandInventoryCapacityCommand, BaseResponse<PlayerWalletDto>>
    {
        private readonly IInventoryService _inventoryService;

        public ExpandInventoryCapacityCommandHandler(IInventoryService inventoryService, IHttpContextAccessor httpContextAccessor)
            : base(httpContextAccessor)
        {
            _inventoryService = inventoryService;
        }

        public async Task<BaseResponse<PlayerWalletDto>> Handle(ExpandInventoryCapacityCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var userId = GetUserId();
                var result = await _inventoryService.ExpandCapacityAsync(userId, request.SlotsToAdd, cancellationToken);
                return BaseResponse<PlayerWalletDto>.SuccessResponse(result, "Mở rộng ô chứa hành trang thành công.");
            }
            catch (KeyNotFoundException ex)
            {
                return BaseResponse<PlayerWalletDto>.FailResponse(ex.Message, statusCode: 404);
            }
            catch (Exception ex)
            {
                return BaseResponse<PlayerWalletDto>.FailResponse(ex.Message, statusCode: 400);
            }
        }
    }
}
