using Core.Persistence.Repositories;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;
using Core.Application.CQRS;
using Core.Common.Entity.MyCompany.Shared.Responses;
using GAME.Application.Services;
using GAME.Application.Features.Shared.Tower;

namespace GAME.Application.Features.Queries.Tower;

public sealed record GetFloorsQuery(string UserId) : IQuery<BaseResponse<List<TowerFloorDto>>>;

public sealed class GetFloorsQueryHandler(
    TowerBattleExecutor executor,
    IUnitOfWork unitOfWork,
    IGamePlayerService gamePlayerService,
    IFormationSnapshotService formationSnapshotService,
    IFormationPowerQueryService formationPowerQueryService,
    ITowerEventService towerEvents)
    : TowerHandlerSupport(executor, unitOfWork, gamePlayerService, formationSnapshotService, formationPowerQueryService, towerEvents), IQueryHandler<GetFloorsQuery, BaseResponse<List<TowerFloorDto>>>
{
    public async Task<BaseResponse<List<TowerFloorDto>>> Handle(GetFloorsQuery message, CancellationToken ct) => BaseResponse<List<TowerFloorDto>>.SuccessResponse(await ExecuteAsync(message.UserId, ct));
    private Task<List<TowerFloorDto>> ExecuteAsync(string userId, CancellationToken ct = default) => GetFloorsAsync(userId, ct);
}
