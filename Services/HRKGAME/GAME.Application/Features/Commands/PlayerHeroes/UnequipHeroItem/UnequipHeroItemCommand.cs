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

namespace GAME.Application.Features.Commands.PlayerHeroes.UnequipHeroItem
{
    public record UnequipHeroItemCommand(long HeroId, string SlotCode) : ICommand<BaseResponse<PlayerHeroDetailDto>>;

    public class UnequipHeroItemCommandHandler : HRKBaseCommand, ICommandHandler<UnequipHeroItemCommand, BaseResponse<PlayerHeroDetailDto>>
    {
        private readonly IHeroEquipmentService _equipmentService;

        public UnequipHeroItemCommandHandler(
            IHeroEquipmentService equipmentService,
            IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            _equipmentService = equipmentService;
        }

        public async Task<BaseResponse<PlayerHeroDetailDto>> Handle(UnequipHeroItemCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.SlotCode))
            {
                return BaseResponse<PlayerHeroDetailDto>.FailResponse("Vị trí trang bị (SlotCode) không được để trống.", statusCode: 400);
            }

            try
            {
                var userId = GetUserId();
                var detail = await _equipmentService.UnequipHeroItemAsync(
                    userId,
                    request.HeroId,
                    request.SlotCode,
                    cancellationToken);

                return BaseResponse<PlayerHeroDetailDto>.SuccessResponse(detail, $"Tháo trang bị ở vị trí {request.SlotCode.ToUpper()} thành công.");
            }
            catch (KeyNotFoundException ex)
            {
                return BaseResponse<PlayerHeroDetailDto>.FailResponse(ex.Message, statusCode: 404);
            }
            catch (ArgumentException ex)
            {
                return BaseResponse<PlayerHeroDetailDto>.FailResponse(ex.Message, statusCode: 400);
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
