using Core.Common.CQRS;
using Core.Common.Entity.MyCompany.Shared.Responses;
using CoreEngine.CQRS;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;
using Microsoft.AspNetCore.Http;

namespace GAME.Application.Features.Queries.PlayerHeroes
{
    public record GetPlayerHeroesQuery : IQuery<BaseResponse<List<PlayerHeroDto>>>;

    public class GetPlayerHeroesQueryHandler : HRKBaseQuery, IQueryHandler<GetPlayerHeroesQuery, BaseResponse<List<PlayerHeroDto>>>
    {
        private readonly IGamePlayerService _playerService;

        public GetPlayerHeroesQueryHandler(IGamePlayerService playerService, IHttpContextAccessor httpContextAccessor)
            : base(httpContextAccessor)
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
