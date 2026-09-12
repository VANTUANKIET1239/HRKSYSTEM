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

namespace GAME.Application.Features.Commands.Inventory.SellItems
{
    public record SellItemsCommand(List<SellItemRequestItem> Items) : ICommand<BaseResponse<SellItemsResponseDto>>;

    public class SellItemsCommandHandler : HRKBaseCommand, ICommandHandler<SellItemsCommand, BaseResponse<SellItemsResponseDto>>
    {
        private readonly IInventoryService _inventoryService;

        public SellItemsCommandHandler(IInventoryService inventoryService, IHttpContextAccessor httpContextAccessor)
            : base(httpContextAccessor)
        {
            _inventoryService = inventoryService;
        }

        public async Task<BaseResponse<SellItemsResponseDto>> Handle(SellItemsCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var userId = GetUserId();
                var result = await _inventoryService.SellItemsAsync(userId, request.Items, cancellationToken);
                return BaseResponse<SellItemsResponseDto>.SuccessResponse(result, "Bán vật phẩm thành công.");
            }
            catch (KeyNotFoundException ex)
            {
                return BaseResponse<SellItemsResponseDto>.FailResponse(ex.Message, statusCode: 404);
            }
            catch (Exception ex)
            {
                return BaseResponse<SellItemsResponseDto>.FailResponse(ex.Message, statusCode: 400);
            }
        }
    }
}
