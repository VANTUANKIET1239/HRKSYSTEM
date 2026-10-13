using Core.Persistence.Repositories;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;
using Core.Application.CQRS;
using Core.Common.Entity.MyCompany.Shared.Responses;
using GAME.Application.Services;
using GAME.Application.Features.Shared.Tower;

namespace GAME.Application.Features.Queries.Tower;

public sealed record GetPendingRewardsQuery(string UserId) : IQuery<BaseResponse<List<PlayerPendingRewardDto>>>;

public sealed class GetPendingRewardsQueryHandler(
    TowerBattleExecutor executor,
    IUnitOfWork unitOfWork,
    IGamePlayerService gamePlayerService,
    IFormationSnapshotService formationSnapshotService,
    IFormationPowerQueryService formationPowerQueryService,
    IEventPeriodService eventPeriodService)
    : TowerHandlerSupport(executor, unitOfWork, gamePlayerService, formationSnapshotService, formationPowerQueryService, eventPeriodService), IQueryHandler<GetPendingRewardsQuery, BaseResponse<List<PlayerPendingRewardDto>>>
{
    public async Task<BaseResponse<List<PlayerPendingRewardDto>>> Handle(GetPendingRewardsQuery message, CancellationToken ct) => BaseResponse<List<PlayerPendingRewardDto>>.SuccessResponse(await ExecuteAsync(message.UserId, ct));
    private Task<List<PlayerPendingRewardDto>> ExecuteAsync(string userId, CancellationToken ct = default) => GetPendingRewardsAsync(userId, ct);
}
