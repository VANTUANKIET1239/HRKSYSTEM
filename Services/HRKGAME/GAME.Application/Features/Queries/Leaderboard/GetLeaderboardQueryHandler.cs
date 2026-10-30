using Core.Application.CQRS;
using Core.Common.Entity.MyCompany.Shared.Responses;
using Core.Web.CQRS;
using GAME.Application.DTOs.Leaderboard;
using GAME.Application.Interfaces;
using Microsoft.AspNetCore.Http;

namespace GAME.Application.Features.Queries.Leaderboard;

public sealed class GetLeaderboardQueryHandler(IHttpContextAccessor http, ILeaderboardReader reader)
    : HRKBaseQuery<GetLeaderboardQuery, BaseResponse<LeaderboardResponse>>(http)
{
    public override async Task<BaseResponse<LeaderboardResponse>> Handle(GetLeaderboardQuery request, CancellationToken ct) =>
        BaseResponse<LeaderboardResponse>.SuccessResponse(await reader.GetAsync(GetUserId(), request.Type, request.Page, request.PageSize, ct));
}
