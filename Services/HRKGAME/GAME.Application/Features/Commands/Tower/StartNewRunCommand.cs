using Core.Common.Errors;
using Core.Persistence.Repositories;
using GAME.Application.Common;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;
using GAME.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Core.Application.CQRS;
using Core.Common.Entity.MyCompany.Shared.Responses;
using GAME.Application.Services;
using GAME.Application.Features.Shared.Tower;

namespace GAME.Application.Features.Commands.Tower;

public sealed record StartNewRunCommand(string UserId) : ICommand<BaseResponse<PlayerEventProgressDto>>;

public sealed class StartNewRunCommandHandler(
    TowerBattleExecutor executor,
    IUnitOfWork unitOfWork,
    IGamePlayerService gamePlayerService,
    IFormationSnapshotService formationSnapshotService,
    IFormationPowerQueryService formationPowerQueryService,
    IEventPeriodService eventPeriodService,
    TowerOperationRunner runner)
    : TowerHandlerSupport(executor, unitOfWork, gamePlayerService, formationSnapshotService, formationPowerQueryService, eventPeriodService), ICommandHandler<StartNewRunCommand, BaseResponse<PlayerEventProgressDto>>
{
    private readonly TowerOperationRunner _runner = runner;
    public async Task<BaseResponse<PlayerEventProgressDto>> Handle(StartNewRunCommand message, CancellationToken ct) => BaseResponse<PlayerEventProgressDto>.SuccessResponse(await ExecuteAsync(message.UserId, ct));
    private Task<PlayerEventProgressDto> ExecuteAsync(string userId, CancellationToken ct = default) => _runner.RunAsync(userId, () => StartNewRunAsyncCore(userId, ct), ct);
    private async Task<PlayerEventProgressDto> StartNewRunAsyncCore(string userId, CancellationToken ct = default)
    {
        var player = await _gamePlayerService.GetPlayerByUserIdAsync(userId, ct, tracked: true) ?? throw new BusinessException("PLAYER_NOT_FOUND", "Không tìm thấy người chơi.", BusinessErrorKind.NotFound);
        var ev = await GetTowerEventAsync(ct);
        var period = await _eventPeriodService.GetOrCreateCurrentPeriodAsync(ev, ct);
        var progress = await _eventPeriodService.GetOrCreatePlayerPeriodProgressAsync(player.Id, ev, period, ct);
        EventAccessPolicy.EnsureCanEnter(ev, player.Level, DateTime.UtcNow);
        EventAccessPolicy.EnsureNewRun(ev, progress);
        if (await _unitOfWork.ReadOnlyRepository<HrkTowerQuickClimbJob>().Query().AnyAsync(j => j.PlayerId == player.Id && (j.Status == "QUEUED" || j.Status == "PROCESSING"), ct))
            throw new BusinessException("QUICK_CLIMB_ACTIVE", "Hãy dừng phiên leo nhanh trước.", BusinessErrorKind.Conflict);
        progress.CurrentFloor = 1;
        progress.RemainingLives = ev.InitialLives;
        progress.CurrentRunNumber++;
        progress.IsCompleted = false;
        progress.UpdatedOn = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync(ct);
        return await GetPlayerProgressAsync(userId, ct);
    }
}
