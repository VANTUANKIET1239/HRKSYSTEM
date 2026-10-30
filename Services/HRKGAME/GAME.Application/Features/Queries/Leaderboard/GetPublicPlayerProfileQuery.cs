using Core.Application.CQRS;
using Core.Common.Entity.MyCompany.Shared.Responses;
using Core.Web.CQRS;
using GAME.Application.DTOs.Leaderboard;
using GAME.Application.Interfaces;
using Microsoft.AspNetCore.Http;

namespace GAME.Application.Features.Queries.Leaderboard;

public sealed record GetPublicPlayerProfileQuery(long PlayerId) : IQuery<BaseResponse<PublicPlayerProfileResponse>>;
