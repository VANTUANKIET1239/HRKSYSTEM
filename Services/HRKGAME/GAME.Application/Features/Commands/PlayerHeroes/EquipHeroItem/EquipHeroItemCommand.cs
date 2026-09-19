using Core.Common.CQRS;
using Core.Common.Entity.MyCompany.Shared.Responses;
using CoreEngine.CQRS;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GAME.Application.Features.Commands.PlayerHeroes.EquipHeroItem
{
    public record EquipHeroItemCommand(long HeroId, EquipHeroItemRequestDto Request) : ICommand<BaseResponse<PlayerHeroDetailDto>>;

    public class EquipHeroItemCommandHandler : HRKBaseCommand, ICommandHandler<EquipHeroItemCommand, BaseResponse<PlayerHeroDetailDto>>
    {
        private readonly IHeroEquipmentService _equipmentService;

        public EquipHeroItemCommandHandler(
            IHeroEquipmentService equipmentService,
            IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            _equipmentService = equipmentService;
        }

        public async Task<BaseResponse<PlayerHeroDetailDto>> Handle(EquipHeroItemCommand request, CancellationToken cancellationToken)
        {
            if (request.Request == null || request.Request.InventoryItemId <= 0)
            {
                return BaseResponse<PlayerHeroDetailDto>.FailResponse("ID trang bị trong hành trang không hợp lệ.", statusCode: 400);
            }

            try
            {
                var userId = GetUserId();
                var detail = await _equipmentService.EquipHeroItemAsync(
                    userId,
                    request.HeroId,
                    request.Request.InventoryItemId,
                    cancellationToken);

                return BaseResponse<PlayerHeroDetailDto>.SuccessResponse(detail, "Mặc trang bị cho võ tướng thành công.");
            }
            catch (KeyNotFoundException ex)
            {
                return BaseResponse<PlayerHeroDetailDto>.FailResponse(ex.Message, statusCode: 404);
            }
            catch (InvalidOperationException ex)
            {
                return BaseResponse<PlayerHeroDetailDto>.FailResponse(ex.Message, statusCode: 400);
            }
            catch (Exception ex)
            {
                return BaseResponse<PlayerHeroDetailDto>.FailResponse(ex.Message, statusCode: 500);
            }
        }
    }
}
