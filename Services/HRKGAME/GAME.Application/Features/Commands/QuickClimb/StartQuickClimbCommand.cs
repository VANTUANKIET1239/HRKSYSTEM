using static GAME.Application.Features.Shared.QuickClimb.QuickClimbProjection;
using Core.Common.Errors;
using Core.Persistence.Repositories;
using Core.TransactionalMessaging.Outbox;
using GAME.Application.Common;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;
using GAME.Application.Events;
using GAME.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Core.Application.CQRS;
using Core.Common.Entity.MyCompany.Shared.Responses;
using GAME.Application.Services;
using GAME.Application.Features.Shared.QuickClimb;

namespace GAME.Application.Features.Commands.QuickClimb;

public sealed record StartQuickClimbCommand(string UserId, StartQuickClimbRequestDto Request) : ICommand<BaseResponse<QuickClimbJobStatusDto>>;

public sealed class StartQuickClimbCommandHandler(
    IOutboxWriter outbox,
    IPlayerActivityEvents activityEvents,
    TowerOperationRunner runner,
    TowerBattleExecutor executor,
    IUnitOfWork unitOfWork,
    IGamePlayerService gamePlayerService,
    ITowerEventService towerEvents,
    IFormationSnapshotService formationSnapshotService,
    ILogger<StartQuickClimbCommandHandler> logger)
    : QuickClimbHandlerSupport(outbox, activityEvents), ICommandHandler<StartQuickClimbCommand, BaseResponse<QuickClimbJobStatusDto>>
{
    private readonly TowerOperationRunner _runner = runner;
    private readonly TowerBattleExecutor _executor = executor;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IGamePlayerService _gamePlayerService = gamePlayerService;
    private readonly ITowerEventService _towerEvents = towerEvents;
    private readonly IFormationSnapshotService _formationSnapshotService = formationSnapshotService;
    private readonly ILogger<StartQuickClimbCommandHandler> _logger = logger;
    public async Task<BaseResponse<QuickClimbJobStatusDto>> Handle(StartQuickClimbCommand message, CancellationToken ct) => BaseResponse<QuickClimbJobStatusDto>.SuccessResponse(await ExecuteAsync(message.UserId, message.Request, ct));
    private async Task<QuickClimbJobStatusDto> ExecuteAsync(string userId, StartQuickClimbRequestDto request, CancellationToken ct = default)
    {
        long? createdPlayerId = null;
        var result = await _runner.RunAsync(userId, () =>
        {
            createdPlayerId = null;
            return StartQuickClimbAsyncCore(userId, request, id => createdPlayerId = id, ct);
        }, ct);
        _logger.Log(createdPlayerId.HasValue ? LogLevel.Information : LogLevel.Debug, "{EventName}: JobId={JobId}, UserId={UserId}, PlayerId={PlayerId}, Status={Status}, TargetFloor={TargetFloor}, DailyRunNumber={DailyRunNumber}", createdPlayerId.HasValue ? "QuickClimbJobCreated" : "QuickClimbJobReused", result.JobId, userId, createdPlayerId, result.Status, result.TargetFloor, result.DailyRunNumber);
        return result;
    }

    private async Task<QuickClimbJobStatusDto> StartQuickClimbAsyncCore(string userId, StartQuickClimbRequestDto request, Action<long> onCreated, CancellationToken ct = default)
    {
        var player = await _gamePlayerService.GetPlayerByUserIdAsync(userId, ct, tracked: true) ?? throw new BusinessException("PLAYER_NOT_FOUND", "Không tìm thấy người chơi.", BusinessErrorKind.NotFound);
        var ev = await GetTowerEventAsync(ct);
        EventAccessPolicy.EnsureCanEnter(ev, player.Level, DateTime.UtcNow);
        var currentPeriod = await _towerEvents.GetOrCreateCurrentPeriodAsync(ev, ct);
        var progress = await _towerEvents.GetOrCreatePlayerPeriodProgressAsync(player.Id, ev, currentPeriod, ct);
        // Check if player already has an active job
        var existingJob = await _unitOfWork.Repository<HrkTowerQuickClimbJob>().Query()
            .Include(x => x.EventPeriod)
            .FirstOrDefaultAsync(j => j.PlayerId == player.Id && (j.Status == "QUEUED" || j.Status == "PROCESSING"), ct);
        if (existingJob != null)
        {
            if (existingJob.EventPeriodId == currentPeriod.Id)
            {
                // Repeated start requests return the same job without consuming another daily run.
                return MapJobToDto(existingJob);
            }

            existingJob.Status = "EXPIRED";
            existingJob.StopReason = "PERIOD_EXPIRED";
            existingJob.CompletedOnUtc = DateTime.UtcNow;
            existingJob.UpdatedOnUtc = DateTime.UtcNow;
            existingJob.Version++;
            EnqueueStatus(existingJob, userId);
            await _unitOfWork.SaveChangesAsync(ct);
        }

        var rules = TowerRules.Parse(ev.RulesJson);
        if (progress.QuickClimbRunsUsed >= rules.QuickClimbDailyLimit)
        {
            throw BusinessException.Localized("QUICK_CLIMB_DAILY_LIMIT", $"Đã sử dụng hết {rules.QuickClimbDailyLimit} lượt leo nhanh trong kỳ hôm nay.", BusinessErrorKind.Conflict);
        }

        // Build frozen snapshot
        var snapshot = await _formationSnapshotService.BuildAsync(player.Id, request.FormationCode, request.Positions, ct);
        if (snapshot.Heroes.Count == 0)
            throw new BusinessException("EMPTY_FORMATION", "Đội hình xuất chiến không có võ tướng nào.", BusinessErrorKind.Conflict);
        await _executor.FreezeSkillsAsync(snapshot, ct, force: true);
        progress.QuickClimbRunsUsed++;
        progress.UpdatedOn = DateTime.UtcNow;
        var job = new HrkTowerQuickClimbJob
        {
            JobId = Guid.NewGuid().ToString("N"),
            PlayerId = player.Id,
            EventPeriodId = currentPeriod.Id,
            Status = "QUEUED",
            StartFloor = 1,
            CurrentFloor = 1,
            TargetFloor = await _executor.MaxFloorAsync(ev.Id, ct),
            InitialLives = 1,
            RemainingLives = 1,
            ClearedFloorsCount = 0,
            DailyRunNumber = progress.QuickClimbRunsUsed,
            FormationCode = snapshot.FormationCode,
            FormationSnapshotJson = JsonSerializer.Serialize(snapshot),
            AccumulatedRewardsJson = "[]",
            LogsJson = "[]",
            CreatedOnUtc = DateTime.UtcNow,
            UpdatedOnUtc = DateTime.UtcNow,
            Version = 1
        };
        await _unitOfWork.Repository<HrkTowerQuickClimbJob>().AddAsync(job);
        job.EventPeriod = currentPeriod;
        _activityEvents.Raise(new QuickClimbStartedEvent(player.Id, userId, job.JobId, job.StartFloor, job.CurrentFloor, job.TargetFloor, job.ClearedFloorsCount, job.Version)
        { GameEventId = ev.Id, EventPeriodId = currentPeriod.Id, PeriodKey = currentPeriod.PeriodKey });
        EnqueueStatus(job, userId);
        EnqueueFloorCommand(job, userId);
        await _unitOfWork.SaveChangesAsync(ct);
        onCreated(player.Id);
        return MapJobToDto(job);
    }

    private async Task<HrkGameEvent> GetTowerEventAsync(CancellationToken ct) => await _unitOfWork.ReadOnlyRepository<HrkGameEvent>().Query()
        .FirstOrDefaultAsync(e => e.Code == "TOWER_CLIMB", ct) ?? throw new KeyNotFoundException("Không tìm thấy cấu hình sự kiện 'TOWER_CLIMB'.");
}
