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
