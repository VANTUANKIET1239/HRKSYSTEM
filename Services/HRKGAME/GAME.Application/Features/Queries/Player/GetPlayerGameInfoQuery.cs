using Core.Application.CQRS;
using Core.Common.Entity.MyCompany.Shared.Responses;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;

namespace GAME.Application.Features.Queries.Player;

public sealed record GetPlayerGameInfoQuery(string UserId) : IQuery<BaseResponse<PlayerGameInfoDto?>>;

public sealed class GetPlayerGameInfoQueryHandler(IPlayerReadModels reader) : IQueryHandler<GetPlayerGameInfoQuery, BaseResponse<PlayerGameInfoDto?>>
{
    public async Task<BaseResponse<PlayerGameInfoDto?>> Handle(GetPlayerGameInfoQuery message, CancellationToken ct) => BaseResponse<PlayerGameInfoDto?>.SuccessResponse(await reader.GetPlayerGameInfoAsync(message.UserId, ct));
}
