using Core.Common.CQRS;
using Core.Common.Entity.MyCompany.Shared.Responses;
using CoreEngine.CQRS;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;
using Microsoft.AspNetCore.Http;

namespace GAME.Application.Features.Queries.Player
{
    public record GetPlayerWalletQuery : IQuery<BaseResponse<PlayerWalletDto>>;

    public class GetPlayerWalletQueryHandler : HRKBaseQuery, IQueryHandler<GetPlayerWalletQuery, BaseResponse<PlayerWalletDto>>
    {
        private readonly IGamePlayerService _playerService;

        public GetPlayerWalletQueryHandler(IGamePlayerService playerService, IHttpContextAccessor httpContextAccessor)
            : base(httpContextAccessor)
        {
            _playerService = playerService;
        }

        public async Task<BaseResponse<PlayerWalletDto>> Handle(GetPlayerWalletQuery request, CancellationToken cancellationToken)
        {
            var userId = GetUserId();
            var wallet = await _playerService.GetPlayerWalletAsync(userId, cancellationToken);
            if (wallet == null)
            {
                return BaseResponse<PlayerWalletDto>.FailResponse("Player wallet not found.", statusCode: 404);
            }
            return BaseResponse<PlayerWalletDto>.SuccessResponse(wallet, "Retrieved player wallet successfully.");
        }
    }
}
