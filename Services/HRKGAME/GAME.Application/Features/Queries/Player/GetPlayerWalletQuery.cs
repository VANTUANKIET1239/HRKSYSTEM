using GAME.Application.Common;
using Core.Common.Entity.MyCompany.Shared.Responses;
using Core.Application.CQRS;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;

namespace GAME.Application.Features.Queries.Player
{
    public record GetPlayerWalletQuery : IQuery<BaseResponse<PlayerWalletDto>>;
    public class GetPlayerWalletQueryHandler : AuthenticatedGameHandler, IQueryHandler<GetPlayerWalletQuery, BaseResponse<PlayerWalletDto>>
    {
        private readonly IPlayerReadModels _playerService;
        public GetPlayerWalletQueryHandler(IPlayerReadModels playerService, IGameRequestContext requestContext) : base(requestContext)
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
