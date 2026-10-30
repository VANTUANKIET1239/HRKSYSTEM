using Core.Application.CQRS;
using Core.Common.Entity.MyCompany.Shared.Responses;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;

namespace GAME.Application.Features.Queries.Player;

public sealed record GetPlayerGameInfoQuery(string UserId) : IQuery<BaseResponse<PlayerGameInfoDto?>>;
