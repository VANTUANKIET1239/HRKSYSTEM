using Core.Application.CQRS;
using Core.Common.Entity.MyCompany.Shared.Responses;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;

namespace GAME.Application.Features.Queries.Player;

public sealed class GetPlayerAvatarsQueryHandler(IPlayerReadModels reader) : IQueryHandler<GetPlayerAvatarsQuery, BaseResponse<List<PlayerAvatarTemplateDto>>>
{
    public async Task<BaseResponse<List<PlayerAvatarTemplateDto>>> Handle(GetPlayerAvatarsQuery message, CancellationToken ct) => BaseResponse<List<PlayerAvatarTemplateDto>>.SuccessResponse(await reader.GetAvatarTemplatesAsync(message.UserId, ct));
}
