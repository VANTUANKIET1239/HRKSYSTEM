using static GAME.Application.Features.Shared.QuickClimb.QuickClimbProjection;
using Core.Common.Errors;
using Core.Persistence.Repositories;
using Core.TransactionalMessaging.Outbox;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;
using GAME.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Core.Application.CQRS;
using Core.Common.Entity.MyCompany.Shared.Responses;
using GAME.Application.Services;
using GAME.Application.Features.Shared.QuickClimb;

namespace GAME.Application.Features.Commands.QuickClimb;

public sealed record StopQuickClimbCommand(string UserId, string JobId) : ICommand<BaseResponse<QuickClimbJobStatusDto>>;

public sealed class StopQuickClimbCommandHandler(
    IOutboxWriter outbox,
    IPlayerActivityEvents activityEvents,
    TowerOperationRunner runner,
    IUnitOfWork unitOfWork,
    IGamePlayerService gamePlayerService)
    : QuickClimbHandlerSupport(outbox, activityEvents), ICommandHandler<StopQuickClimbCommand, BaseResponse<QuickClimbJobStatusDto>>
{
    private readonly TowerOperationRunner _runner = runner;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IGamePlayerService _gamePlayerService = gamePlayerService;
    public async Task<BaseResponse<QuickClimbJobStatusDto>> Handle(StopQuickClimbCommand message, CancellationToken ct) => BaseResponse<QuickClimbJobStatusDto>.SuccessResponse(await ExecuteAsync(message.UserId, message.JobId, ct));
    private Task<QuickClimbJobStatusDto> ExecuteAsync(string userId, string jobId, CancellationToken ct = default) => _runner.RunAsync(userId, () => StopQuickClimbAsyncCore(userId, jobId, ct), ct);
    private async Task<QuickClimbJobStatusDto> StopQuickClimbAsyncCore(string userId, string jobId, CancellationToken ct = default)
    {
        var player = await _gamePlayerService.GetPlayerByUserIdAsync(userId, ct, tracked: true) ?? throw new BusinessException("PLAYER_NOT_FOUND", "Không tìm thấy người chơi.", BusinessErrorKind.NotFound);
        var job = await _unitOfWork.Repository<HrkTowerQuickClimbJob>().Query()
            .Include(x => x.EventPeriod)
            .FirstOrDefaultAsync(j => j.JobId == jobId && j.PlayerId == player.Id, ct) ?? throw BusinessException.Localized("QUICK_CLIMB_JOB_NOT_FOUND", $"Không tìm thấy phiên leo nhanh '{jobId}'.", BusinessErrorKind.NotFound);
        if (job.Status == "QUEUED")
        {
            job.Status = "CANCELLED";
            job.StopReason = "USER_CANCELLED";
            job.CompletedOnUtc = DateTime.UtcNow;
            job.UpdatedOnUtc = DateTime.UtcNow;
            job.Version++;
            EnqueueStatus(job, userId);
            await _unitOfWork.SaveChangesAsync(ct);
        }
        else if (job.Status == "PROCESSING")
        {
            // Mark requested cancel; worker will finish accepted in-flight battle and stop cleanly
            job.StopReason = "USER_CANCELLED";
            job.UpdatedOnUtc = DateTime.UtcNow;
            await _unitOfWork.SaveChangesAsync(ct);
        }

        return MapJobToDto(job);
    }
}
