using Core.Common.Errors;
using Core.Persistence.Repositories;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;
using Core.Application.CQRS;
using Core.Common.Entity.MyCompany.Shared.Responses;
using GAME.Application.Features.Shared.Formation;

namespace GAME.Application.Features.Queries.Formation;

public sealed record GetFormationDetailQuery(string UserId, string FormationCode) : IQuery<BaseResponse<FormationDetailDto?>>;

public sealed class GetFormationDetailQueryHandler(
    IUnitOfWork unitOfWork,
    IGamePlayerService gamePlayerService,
    IFormationStatService formationStatService,
    ICombatPowerService combatPowerService,
    IHeroStatCalculationService heroStatCalculationService)
    : FormationHandlerSupport(unitOfWork, gamePlayerService, formationStatService, combatPowerService, heroStatCalculationService), IQueryHandler<GetFormationDetailQuery, BaseResponse<FormationDetailDto?>>
{
    public async Task<BaseResponse<FormationDetailDto?>> Handle(GetFormationDetailQuery message, CancellationToken ct) => BaseResponse<FormationDetailDto?>.SuccessResponse(await ExecuteAsync(message.UserId, message.FormationCode, ct) ?? throw new BusinessException("FORMATION_NOT_FOUND", $"Không tìm thấy thông tin trận pháp '{message.FormationCode}'.", BusinessErrorKind.NotFound));
    private Task<FormationDetailDto?> ExecuteAsync(string userId, string formationCode, CancellationToken cancellationToken = default) => GetFormationDetailAsync(userId, formationCode, cancellationToken);
}
