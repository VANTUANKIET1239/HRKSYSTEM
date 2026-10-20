using Core.Common.Errors;
using Core.Persistence.Repositories;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;
using Core.Application.CQRS;
using Core.Common.Entity.MyCompany.Shared.Responses;
using GAME.Application.Services;
using GAME.Application.Features.Shared.Tower;

namespace GAME.Application.Features.Queries.Tower;

public sealed record GetBattleDetailQuery(string UserId, string BattleId) : IQuery<BaseResponse<StartTowerBattleResultDto?>>;

public sealed class GetBattleDetailQueryHandler(
    TowerBattleExecutor executor,
    IUnitOfWork unitOfWork,
    IGamePlayerService gamePlayerService,
    IFormationSnapshotService formationSnapshotService,
    IFormationPowerQueryService formationPowerQueryService,
    ITowerEventService towerEvents)
    : TowerHandlerSupport(executor, unitOfWork, gamePlayerService, formationSnapshotService, formationPowerQueryService, towerEvents), IQueryHandler<GetBattleDetailQuery, BaseResponse<StartTowerBattleResultDto?>>
{
    public async Task<BaseResponse<StartTowerBattleResultDto?>> Handle(GetBattleDetailQuery message, CancellationToken ct) => BaseResponse<StartTowerBattleResultDto?>.SuccessResponse(await ExecuteAsync(message.UserId, message.BattleId, ct) ?? throw new BusinessException("BATTLE_NOT_FOUND", $"Không tìm thấy trận đánh.", BusinessErrorKind.NotFound));
    private Task<StartTowerBattleResultDto?> ExecuteAsync(string userId, string battleId, CancellationToken ct = default) => GetBattleDetailAsync(userId, battleId, ct);
}
