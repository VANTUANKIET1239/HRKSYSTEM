using GAME.Application.Common;
using Core.Common.Entity.MyCompany.Shared.Responses;
using Core.Application.CQRS;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;

namespace GAME.Application.Features.Queries.PlayerHeroes
{
    public class GetPlayerHeroesQueryHandler : AuthenticatedGameHandler, IQueryHandler<GetPlayerHeroesQuery, BaseResponse<List<PlayerHeroDto>>>
    {
        private readonly IPlayerReadModels _playerService;
        public GetPlayerHeroesQueryHandler(IPlayerReadModels playerService, IGameRequestContext requestContext) : base(requestContext)
        {
            _playerService = playerService;
        }

        public async Task<BaseResponse<List<PlayerHeroDto>>> Handle(GetPlayerHeroesQuery request, CancellationToken cancellationToken)
        {
            var userId = GetUserId();
            var heroes = await _playerService.GetPlayerHeroesAsync(userId, cancellationToken);
            return BaseResponse<List<PlayerHeroDto>>.SuccessResponse(heroes, "Retrieved player heroes successfully.");
        }
    }
}
