using Core.Application.CQRS;
using Core.Common.Entity.MyCompany.Shared.Responses;
using Core.Web.CQRS;
using GAME.Application.DTOs.Leaderboard;
using GAME.Application.Interfaces;
using Microsoft.AspNetCore.Http;

namespace GAME.Application.Features.Queries.Leaderboard;

public sealed class GetPublicPlayerProfileQueryHandler(IHttpContextAccessor http, ILeaderboardReader reader)
    : HRKBaseQuery<GetPublicPlayerProfileQuery, BaseResponse<PublicPlayerProfileResponse>>(http)
{
    public override async Task<BaseResponse<PublicPlayerProfileResponse>> Handle(GetPublicPlayerProfileQuery request, CancellationToken ct)
    {
        _ = GetUserId();
        return BaseResponse<PublicPlayerProfileResponse>.SuccessResponse(await reader.GetProfileAsync(request.PlayerId, ct));
    }
}
