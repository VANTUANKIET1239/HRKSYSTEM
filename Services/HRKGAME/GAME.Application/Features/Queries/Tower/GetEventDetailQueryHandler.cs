using Core.Common.Errors;
using Core.Persistence.Repositories;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;
using Core.Application.CQRS;
using Core.Common.Entity.MyCompany.Shared.Responses;
using GAME.Application.Services;
using GAME.Application.Features.Shared.Tower;

namespace GAME.Application.Features.Queries.Tower;

public sealed class GetEventDetailQueryHandler(
    TowerBattleExecutor executor,
    IUnitOfWork unitOfWork,
    IGamePlayerService gamePlayerService,
    IFormationSnapshotService formationSnapshotService,
    IFormationPowerQueryService formationPowerQueryService,
    ITowerEventService towerEvents)
    : TowerHandlerSupport(executor, unitOfWork, gamePlayerService, formationSnapshotService, formationPowerQueryService, towerEvents), IQueryHandler<GetEventDetailQuery, BaseResponse<GameEventDto>>
{
    public async Task<BaseResponse<GameEventDto>> Handle(GetEventDetailQuery message, CancellationToken ct) => BaseResponse<GameEventDto>.SuccessResponse(await ExecuteAsync(message.UserId, message.EventCode, ct));
    private Task<GameEventDto> ExecuteAsync(string userId, string eventCode, CancellationToken ct = default) => GetEventDetailAsyncCore(userId, eventCode, ct);
    private async Task<GameEventDto> GetEventDetailAsyncCore(string userId, string eventCode, CancellationToken ct = default)
    {
        var events = await GetEventsAsync(userId, ct);
        var ev = events.FirstOrDefault(e => e.Code.Equals(eventCode, StringComparison.OrdinalIgnoreCase)) ?? throw BusinessException.Localized("EVENT_NOT_FOUND", $"Không tìm thấy sự kiện '{eventCode}'.", BusinessErrorKind.NotFound);
        return ev;
    }
}
