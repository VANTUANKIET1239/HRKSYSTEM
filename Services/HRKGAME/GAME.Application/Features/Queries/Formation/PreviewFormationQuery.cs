using Core.Application.CQRS;
using Core.Common.Errors;
using Core.Common.Entity.MyCompany.Shared.Responses;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;

namespace GAME.Application.Features.Queries.Formation;

public sealed record PreviewFormationQuery(

    string UserId,

    string FormationCode,

    IReadOnlyCollection<FormationPositionRequestDto> Positions

)

    : IQuery<BaseResponse<FormationPreviewResponseDto>>;

public sealed class PreviewFormationQueryHandler(IGamePlayerService players, IFormationSnapshotService snapshots) : IQueryHandler<PreviewFormationQuery, BaseResponse<FormationPreviewResponseDto>>
{
    public async Task<BaseResponse<FormationPreviewResponseDto>> Handle(PreviewFormationQuery message, CancellationToken ct)
    {
        var player = await players.GetPlayerByUserIdAsync(message.UserId, ct) ?? throw new BusinessException("PLAYER_NOT_FOUND", "Player was not found.", BusinessErrorKind.NotFound);
        return BaseResponse<FormationPreviewResponseDto>.SuccessResponse(await snapshots.PreviewAsync(player.Id, message.FormationCode, message.Positions, ct));
    }
}
