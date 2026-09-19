using Core.Common.CQRS;
using Core.Common.Entity.MyCompany.Shared.Responses;
using CoreEngine.CQRS;
using GAME.Application.Interfaces;
using Microsoft.AspNetCore.Http;

namespace GAME.Application.Features.Commands.PlayerHeroes
{
    public record UpdatePlayerHeroFlagsRequest(bool? IsLocked, bool? IsFavorite);
    public record UpdatePlayerHeroFlagsCommand(long HeroId, UpdatePlayerHeroFlagsRequest Request) : ICommand<BaseResponse<PlayerHeroFlagsDto>>;
    public record PlayerHeroFlagsDto(long HeroId, bool IsLocked, bool IsFavorite);

    public class UpdatePlayerHeroFlagsCommandHandler : HRKBaseCommand, ICommandHandler<UpdatePlayerHeroFlagsCommand, BaseResponse<PlayerHeroFlagsDto>>
    {
        private readonly IGamePlayerService _playerService;

        public UpdatePlayerHeroFlagsCommandHandler(IGamePlayerService playerService, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            _playerService = playerService;
        }

        public async Task<BaseResponse<PlayerHeroFlagsDto>> Handle(UpdatePlayerHeroFlagsCommand request, CancellationToken cancellationToken)
        {
            if (request.Request.IsLocked is null && request.Request.IsFavorite is null)
                return BaseResponse<PlayerHeroFlagsDto>.FailResponse("Can cung cap IsLocked hoac IsFavorite.", statusCode: 400);

            try
            {
                var userId = GetUserId();
                var detail = await _playerService.GetPlayerHeroDetailAsync(userId, request.HeroId, cancellationToken);
                if (detail is null)
                    return BaseResponse<PlayerHeroFlagsDto>.FailResponse("Khong tim thay vo tuong.", statusCode: 404);

                var isLocked = request.Request.IsLocked is null ? detail.IsLocked : await _playerService.SetPlayerHeroLockAsync(userId, request.HeroId, request.Request.IsLocked.Value, cancellationToken);
                var isFavorite = request.Request.IsFavorite is null ? detail.IsFavorite : await _playerService.SetPlayerHeroFavoriteAsync(userId, request.HeroId, request.Request.IsFavorite.Value, cancellationToken);
                return BaseResponse<PlayerHeroFlagsDto>.SuccessResponse(new(request.HeroId, isLocked, isFavorite), "Cap nhat trang thai vo tuong thanh cong.");
            }
            catch (KeyNotFoundException ex)
            {
                return BaseResponse<PlayerHeroFlagsDto>.FailResponse(ex.Message, statusCode: 404);
            }
        }
    }
}
