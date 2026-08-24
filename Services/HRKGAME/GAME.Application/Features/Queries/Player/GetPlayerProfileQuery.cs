using Core.Common.CQRS;
using Core.Common.Entity.MyCompany.Shared.Responses;
using CoreEngine.CQRS;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;
using Microsoft.AspNetCore.Http;

namespace GAME.Application.Features.Queries.Player
{
    public record GetPlayerProfileQuery : IQuery<BaseResponse<PlayerProfileDto>>;

    public class GetPlayerProfileQueryHandler : HRKBaseQuery, IQueryHandler<GetPlayerProfileQuery, BaseResponse<PlayerProfileDto>>
    {
        private readonly IGamePlayerService _playerService;

        public GetPlayerProfileQueryHandler(IGamePlayerService playerService, IHttpContextAccessor httpContextAccessor)
            : base(httpContextAccessor)
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
