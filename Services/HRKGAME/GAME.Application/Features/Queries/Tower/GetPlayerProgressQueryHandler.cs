using Core.Persistence.Repositories;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;
using Core.Application.CQRS;
using Core.Common.Entity.MyCompany.Shared.Responses;
using GAME.Application.Services;
using GAME.Application.Features.Shared.Tower;

namespace GAME.Application.Features.Queries.Tower;

public sealed class GetPlayerProgressQueryHandler(
    TowerBattleExecutor executor,
    IUnitOfWork unitOfWork,
    IGamePlayerService gamePlayerService,
    IFormationSnapshotService formationSnapshotService,
    IFormationPowerQueryService formationPowerQueryService,
    ITowerEventService towerEvents)
    : TowerHandlerSupport(executor, unitOfWork, gamePlayerService, formationSnapshotService, formationPowerQueryService, towerEvents), IQueryHandler<GetPlayerProgressQuery, BaseResponse<PlayerEventProgressDto>>
{
    public async Task<BaseResponse<PlayerEventProgressDto>> Handle(GetPlayerProgressQuery message, CancellationToken ct) => BaseResponse<PlayerEventProgressDto>.SuccessResponse(await ExecuteAsync(message.UserId, ct));
    private Task<PlayerEventProgressDto> ExecuteAsync(string userId, CancellationToken ct = default) => GetPlayerProgressAsync(userId, ct);
}
