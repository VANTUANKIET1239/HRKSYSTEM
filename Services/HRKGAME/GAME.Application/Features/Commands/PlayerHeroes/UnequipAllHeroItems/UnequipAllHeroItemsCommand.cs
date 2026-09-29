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

namespace GAME.Application.Features.Commands.PlayerHeroes.UnequipAllHeroItems
{
    public record UnequipAllHeroItemsCommand(long HeroId) : ICommand<BaseResponse<PlayerHeroDetailDto>>;

    public class UnequipAllHeroItemsCommandHandler : HRKBaseCommand, ICommandHandler<UnequipAllHeroItemsCommand, BaseResponse<PlayerHeroDetailDto>>
    {
        private readonly IHeroEquipmentService _equipmentService;

        public UnequipAllHeroItemsCommandHandler(
            IHeroEquipmentService equipmentService,
            IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            _equipmentService = equipmentService;
        }

        public async Task<BaseResponse<PlayerHeroDetailDto>> Handle(UnequipAllHeroItemsCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var userId = GetUserId();
                var detail = await _equipmentService.UnequipAllHeroItemsAsync(
                    userId,
                    request.HeroId,
                    cancellationToken);

                return BaseResponse<PlayerHeroDetailDto>.SuccessResponse(detail, "Tháo toàn bộ trang bị thành công.");
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
                return BaseResponse<PlayerHeroDetailDto>.FailResponse($"Lỗi hệ thống khi tháo toàn bộ trang bị: {ex.Message}", statusCode: 500);
            }
        }
    }
}
