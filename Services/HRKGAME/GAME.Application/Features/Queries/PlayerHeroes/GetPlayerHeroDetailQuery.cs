using GAME.Application.Common;
using Core.Common.Entity.MyCompany.Shared.Responses;
using Core.Application.CQRS;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;

namespace GAME.Application.Features.Queries.PlayerHeroes
{
    public record GetPlayerHeroDetailQuery(long HeroId) : IQuery<BaseResponse<PlayerHeroDetailDto>>;
    public class GetPlayerHeroDetailQueryHandler : AuthenticatedGameHandler, IQueryHandler<GetPlayerHeroDetailQuery, BaseResponse<PlayerHeroDetailDto>>
    {
        private readonly IPlayerReadModels _playerService;
        public GetPlayerHeroDetailQueryHandler(IPlayerReadModels playerService, IGameRequestContext requestContext) : base(requestContext)
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
