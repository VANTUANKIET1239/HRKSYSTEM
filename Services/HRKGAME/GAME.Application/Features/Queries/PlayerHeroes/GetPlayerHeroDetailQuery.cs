using Core.Common.CQRS;
using Core.Common.Entity.MyCompany.Shared.Responses;
using CoreEngine.CQRS;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;
using Microsoft.AspNetCore.Http;

namespace GAME.Application.Features.Queries.PlayerHeroes
{
    public record GetPlayerHeroDetailQuery(long HeroId) : IQuery<BaseResponse<PlayerHeroDetailDto>>;

    public class GetPlayerHeroDetailQueryHandler : HRKBaseQuery, IQueryHandler<GetPlayerHeroDetailQuery, BaseResponse<PlayerHeroDetailDto>>
    {
        private readonly IGamePlayerService _playerService;

        public GetPlayerHeroDetailQueryHandler(IGamePlayerService playerService, IHttpContextAccessor httpContextAccessor)
            : base(httpContextAccessor)
        {
            _playerService = playerService;
        }

        public async Task<BaseResponse<PlayerHeroDetailDto>> Handle(GetPlayerHeroDetailQuery request, CancellationToken cancellationToken)
        {
            var userId = GetUserId();
            var detail = await _playerService.GetPlayerHeroDetailAsync(userId, request.HeroId, cancellationToken);
            if (detail == null)
            {
                return BaseResponse<PlayerHeroDetailDto>.FailResponse("Player hero detail not found.", statusCode: 404);
            }
            return BaseResponse<PlayerHeroDetailDto>.SuccessResponse(detail, "Retrieved player hero detail successfully.");
        }
    }
}
