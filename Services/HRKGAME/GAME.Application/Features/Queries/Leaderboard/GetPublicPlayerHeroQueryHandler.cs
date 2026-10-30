using Core.Application.CQRS;
using Core.Common.Entity.MyCompany.Shared.Responses;
using Core.Web.CQRS;
using GAME.Application.DTOs.Leaderboard;
using GAME.Application.Interfaces;
using Microsoft.AspNetCore.Http;

namespace GAME.Application.Features.Queries.Leaderboard;

public sealed class GetPublicPlayerHeroQueryHandler(IHttpContextAccessor http, ILeaderboardReader reader)
    : HRKBaseQuery<GetPublicPlayerHeroQuery, BaseResponse<PublicHeroDetailResponse>>(http)
{
    public override async Task<BaseResponse<PublicHeroDetailResponse>> Handle(GetPublicPlayerHeroQuery request, CancellationToken ct)
    {
        _ = GetUserId();
        return BaseResponse<PublicHeroDetailResponse>.SuccessResponse(await reader.GetHeroAsync(request.PlayerId, request.HeroId, ct));
    }
}
