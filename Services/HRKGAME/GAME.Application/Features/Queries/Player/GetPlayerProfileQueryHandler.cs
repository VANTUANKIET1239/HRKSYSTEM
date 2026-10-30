using GAME.Application.Common;
using Core.Common.Entity.MyCompany.Shared.Responses;
using Core.Application.CQRS;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;

namespace GAME.Application.Features.Queries.Player
{
    public class GetPlayerProfileQueryHandler : AuthenticatedGameHandler, IQueryHandler<GetPlayerProfileQuery, BaseResponse<PlayerProfileDto>>
    {
        private readonly IPlayerReadModels _playerService;
        public GetPlayerProfileQueryHandler(IPlayerReadModels playerService, IGameRequestContext requestContext) : base(requestContext)
        {
            _playerService = playerService;
        }

        public async Task<BaseResponse<PlayerProfileDto>> Handle(GetPlayerProfileQuery request, CancellationToken cancellationToken)
        {
            var userId = GetUserId();
            var profile = await _playerService.GetPlayerProfileAsync(userId, cancellationToken);
            if (profile == null)
            {
                return BaseResponse<PlayerProfileDto>.FailResponse("Player profile not found for this user.", statusCode: 404);
            }

            return BaseResponse<PlayerProfileDto>.SuccessResponse(profile, "Retrieved player profile successfully.");
        }
    }
}
