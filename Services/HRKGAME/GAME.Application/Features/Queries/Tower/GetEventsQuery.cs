using Core.Persistence.Repositories;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;
using Core.Application.CQRS;
using Core.Common.Entity.MyCompany.Shared.Responses;
using GAME.Application.Services;
using GAME.Application.Features.Shared.Tower;

namespace GAME.Application.Features.Queries.Tower;

public sealed record GetEventsQuery(string UserId) : IQuery<BaseResponse<List<GameEventDto>>>;

public sealed class GetEventsQueryHandler(
    TowerBattleExecutor executor,
    IUnitOfWork unitOfWork,
    IGamePlayerService gamePlayerService,
    IFormationSnapshotService formationSnapshotService,
    IFormationPowerQueryService formationPowerQueryService,
    IEventPeriodService eventPeriodService)
    : TowerHandlerSupport(executor, unitOfWork, gamePlayerService, formationSnapshotService, formationPowerQueryService, eventPeriodService), IQueryHandler<GetEventsQuery, BaseResponse<List<GameEventDto>>>
{
    public async Task<BaseResponse<List<GameEventDto>>> Handle(GetEventsQuery message, CancellationToken ct) => BaseResponse<List<GameEventDto>>.SuccessResponse(await ExecuteAsync(message.UserId, ct));
    private Task<List<GameEventDto>> ExecuteAsync(string userId, CancellationToken ct = default) => GetEventsAsync(userId, ct);
}
