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

namespace GAME.Application.Features.Commands.PlayerHeroes.SwapHeroEquipment
{
    public record SwapHeroEquipmentCommand(long SourceHeroId, SwapHeroEquipmentRequestDto Request) : ICommand<BaseResponse<SwapHeroEquipmentResultDto>>;

    public class SwapHeroEquipmentCommandHandler : HRKBaseCommand, ICommandHandler<SwapHeroEquipmentCommand, BaseResponse<SwapHeroEquipmentResultDto>>
    {
        private readonly IHeroEquipmentService _equipmentService;

        public SwapHeroEquipmentCommandHandler(
            IHeroEquipmentService equipmentService,
            IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            _equipmentService = equipmentService;
        }

        public async Task<BaseResponse<SwapHeroEquipmentResultDto>> Handle(SwapHeroEquipmentCommand request, CancellationToken cancellationToken)
        {
            if (request.Request == null || request.Request.TargetHeroId <= 0)
            {
                return BaseResponse<SwapHeroEquipmentResultDto>.FailResponse("Võ tướng đích không hợp lệ.", statusCode: 400);
            }

            if (request.SourceHeroId == request.Request.TargetHeroId)
            {
                return BaseResponse<SwapHeroEquipmentResultDto>.FailResponse("Không thể hoán đổi trang bị với chính võ tướng này.", statusCode: 400);
            }

            try
            {
                var userId = GetUserId();
                var result = await _equipmentService.SwapHeroEquipmentAsync(
                    userId,
                    request.SourceHeroId,
                    request.Request.TargetHeroId,
                    cancellationToken);

                return BaseResponse<SwapHeroEquipmentResultDto>.SuccessResponse(result, "Hoán đổi toàn bộ bộ trang bị giữa hai võ tướng thành công.");
            }
            catch (KeyNotFoundException ex)
            {
                return BaseResponse<SwapHeroEquipmentResultDto>.FailResponse(ex.Message, statusCode: 404);
            }
            catch (ArgumentException ex)
            {
                return BaseResponse<SwapHeroEquipmentResultDto>.FailResponse(ex.Message, statusCode: 400);
            }
            catch (InvalidOperationException ex)
            {
                return BaseResponse<SwapHeroEquipmentResultDto>.FailResponse(ex.Message, statusCode: 400);
            }
            catch (Exception ex)
            {
                return BaseResponse<SwapHeroEquipmentResultDto>.FailResponse($"Lỗi hệ thống khi hoán đổi trang bị: {ex.Message}", statusCode: 500);
            }
        }
    }
}
