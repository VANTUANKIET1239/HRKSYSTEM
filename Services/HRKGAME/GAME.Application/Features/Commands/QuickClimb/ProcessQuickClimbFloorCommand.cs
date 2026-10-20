using static GAME.Application.Features.Shared.QuickClimb.QuickClimbProjection;
using Core.Persistence.Repositories;
using Core.TransactionalMessaging.Inbox;
using Core.TransactionalMessaging.Outbox;
using Microsoft.Data.SqlClient;
using GAME.Application.Common;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;
using GAME.Domain.Entities;
using Oservability.Tracing;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Core.Application.CQRS;
using Core.Common.Entity.MyCompany.Shared.Responses;
using GAME.Application.Services;
using GAME.Application.Features.Shared.QuickClimb;
using GAME.Application.Events;

namespace GAME.Application.Features.Commands.QuickClimb;

public sealed record ProcessQuickClimbFloorCommand(

    Guid MessageId,

    string JobId,

    string UserId,

    int ExpectedFloor,

    long ExpectedVersion,

    string WorkerId

)

    : ICommand<BaseResponse<bool>>;

public sealed class ProcessQuickClimbFloorCommandHandler(
    IOutboxWriter outbox,
    IPlayerActivityEvents activityEvents,
    TowerOperationRunner runner,
    TowerBattleExecutor executor,
    TowerRewardService rewards,
    IUnitOfWork unitOfWork,
    IGamePlayerService gamePlayerService,
    IInboxExecutor inbox,
    ILogger<ProcessQuickClimbFloorCommandHandler> logger)
    : QuickClimbHandlerSupport(outbox, activityEvents), ICommandHandler<ProcessQuickClimbFloorCommand, BaseResponse<bool>>
{
    private readonly TowerOperationRunner _runner = runner;
    private readonly TowerBattleExecutor _executor = executor;
    private readonly TowerRewardService _rewards = rewards;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IGamePlayerService _gamePlayerService = gamePlayerService;
    private readonly ILogger<ProcessQuickClimbFloorCommandHandler> _logger = logger;
    private readonly IInboxExecutor _inbox = inbox;
    public async Task<BaseResponse<bool>> Handle(ProcessQuickClimbFloorCommand message, CancellationToken ct) => BaseResponse<bool>.SuccessResponse(await ExecuteAsync(message.MessageId, message.JobId, message.UserId, message.ExpectedFloor, message.ExpectedVersion, message.WorkerId, ct));
    private async Task<bool> ExecuteAsync(Guid messageId, string jobId, string userId, int expectedFloor, long expectedVersion, string workerId, CancellationToken ct = default)
    {
        const string consumerName = "QuickClimb";
        var messageKey = messageId.ToString("N");
        using var activity = GameOperation.Source.StartActivity("QuickClimb.ProcessFloor", ActivityKind.Internal);
        activity?.SetTag("quickclimb.job.id", jobId);
        activity?.SetTag("quickclimb.floor", expectedFloor);
        activity?.SetTag("messaging.message.id", messageKey);
        var timer = System.Diagnostics.Stopwatch.StartNew();
        HrkTowerQuickClimbJob? processedJob = null;
        try
        {
            var processed = await _runner.RunAsync(userId, async () =>
            {
                // Reset on execution-strategy retries; only report the committed attempt.
                processedJob = null;
                var execution = await _inbox.ExecuteInCurrentTransactionAsync(consumerName, messageKey, async token =>
                {
                    var job = await _unitOfWork.Repository<HrkTowerQuickClimbJob>().Query()
                        .Include(x => x.EventPeriod).SingleAsync(x => x.JobId == jobId, token);
                    if (job.CurrentFloor != expectedFloor || job.Version != expectedVersion || (job.Status != "QUEUED" && job.Status != "PROCESSING"))
                    {
                        return false;
                    }

                    job.Status = "PROCESSING";
                    job.WorkerId = workerId;
                    job.LockedUntilUtc = null;
                    await ProcessSingleFloorIterationAsync(job, token);
                    processedJob = job;
                    job.Version++;
                    EnqueueStatus(job, userId);
                    if (job.Status is "QUEUED" or "PROCESSING")
                    {
                        EnqueueFloorCommand(job, userId);
                    }

                    return true;
                }, ct);
                return !execution.IsDuplicate && execution.Result;
            }, ct);
            if (processed && processedJob is not null)
            {
                activity?.SetTag("quickclimb.status", processedJob.Status);
                var floorLog = ParseLogsJson(processedJob.LogsJson).LastOrDefault(x => x.FloorNumber == expectedFloor);
                activity?.SetTag("quickclimb.floor.result", floorLog is null ? "NOT_RUN" : floorLog.IsVictory ? "VICTORY" : "DEFEAT");
                if (processedJob.Status == "ERROR")
                    activity?.SetStatus(ActivityStatusCode.Error);
                if (floorLog is not null)
                    _logger.LogDebug("QuickClimbFloorProcessed: JobId={JobId}, PlayerId={PlayerId}, Floor={Floor}, Result={Result}, TotalTurns={TotalTurns}, DurationMs={DurationMs}, MessageId={MessageId}", jobId, processedJob.PlayerId, expectedFloor, floorLog.IsVictory ? "VICTORY" : "DEFEAT", floorLog.TotalTurns, timer.Elapsed.TotalMilliseconds, messageId);
                LogCommittedJobEnd(processedJob, messageId);
            }

            activity?.SetTag("quickclimb.processed", processed);
            return processed;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (SqlException exception) when (IsTransientSql(exception))
        {
            activity?.SetStatus(ActivityStatusCode.Error, exception.GetType().Name);
            throw new QuickClimbTransientException("Quick-climb persistence is temporarily unavailable.", exception);
        }
        catch (TimeoutException exception)
        {
            activity?.SetStatus(ActivityStatusCode.Error, exception.GetType().Name);
            throw new QuickClimbTransientException("Quick-climb processing timed out.", exception);
        }
        catch (Exception exception)
        {
            activity?.SetStatus(ActivityStatusCode.Error, exception.GetType().Name);
            _logger.LogError(exception, "Quick climb message {MessageId} failed for job {JobId} at floor {Floor}.", messageId, jobId, expectedFloor);
            HrkTowerQuickClimbJob? failedJob = null;
            var failureRecorded = await _runner.RunAsync(userId, async () =>
            {
                failedJob = null;
                var execution = await _inbox.ExecuteInCurrentTransactionAsync(consumerName, messageKey, async token =>
                {
                    var job = await _unitOfWork.Repository<HrkTowerQuickClimbJob>().Query()
                        .Include(x => x.EventPeriod).SingleAsync(x => x.JobId == jobId, token);
                    if (job.CurrentFloor == expectedFloor && job.Version == expectedVersion && job.Status is "QUEUED" or "PROCESSING")
                    {
                        job.Status = "ERROR";
                        job.StopReason = "ERROR";
                        job.CompletedOnUtc = DateTime.UtcNow;
                        job.UpdatedOnUtc = DateTime.UtcNow;
                        job.Version++;
                        failedJob = job;
                        EnqueueStatus(job, userId, "QUICK_CLIMB_FAILED", "Quick climb could not continue because of a system error.");
                    }

                    return true;
                }, ct);
                return !execution.IsDuplicate && execution.Result;
            }, ct);
            if (failureRecorded && failedJob is not null)
                LogCommittedJobEnd(failedJob, messageId);
            return true;
        }
    }

    private void LogCommittedJobEnd(HrkTowerQuickClimbJob job, Guid messageId)
    {
        if (job.Status is "QUEUED" or "PROCESSING")
            return;
        _logger.LogInformation("QuickClimbEnded: JobId={JobId}, PlayerId={PlayerId}, Status={Status}, ClearedFloorsCount={ClearedFloorsCount}, TargetFloor={TargetFloor}, StopReason={StopReason}, MessageId={MessageId}", job.JobId, job.PlayerId, job.Status, job.ClearedFloorsCount, job.TargetFloor, job.StopReason, messageId);
    }

    private async Task ProcessSingleFloorIterationAsync(HrkTowerQuickClimbJob job, CancellationToken ct)
    {
        // 1. Check if user requested cancel before starting next battle
        if (job.StopReason == "USER_CANCELLED")
        {
            job.Status = "CANCELLED";
            job.CompletedOnUtc = DateTime.UtcNow;
            job.UpdatedOnUtc = DateTime.UtcNow;
            await _unitOfWork.SaveChangesAsync(ct);
            return;
        }

        // 2. Check period expiry (crossing 12:00 PM reset boundary)
        var period = await _unitOfWork.ReadOnlyRepository<HrkEventPeriod>().Query()
            .FirstOrDefaultAsync(p => p.Id == job.EventPeriodId, ct);
        if (period == null || DateTime.UtcNow >= period.EndAtUtc)
        {
            job.Status = "EXPIRED";
            job.StopReason = "PERIOD_EXPIRED";
            job.CompletedOnUtc = DateTime.UtcNow;
            job.UpdatedOnUtc = DateTime.UtcNow;
            await _unitOfWork.SaveChangesAsync(ct);
            return;
        }

        var ev = await _unitOfWork.ReadOnlyRepository<HrkGameEvent>().Query()
            .SingleAsync(e => e.Id == period.EventId, ct);
        if (!EventAccessPolicy.IsOpen(ev, DateTime.UtcNow))
        {
            job.Status = "CANCELLED";
            job.StopReason = "EVENT_CLOSED";
            job.CompletedOnUtc = DateTime.UtcNow;
            return;
        }

        // 3. Check lives
        if (job.RemainingLives <= 0)
        {
            job.Status = "STOPPED_DEFEAT";
            job.StopReason = "FIRST_DEFEAT";
            job.CompletedOnUtc = DateTime.UtcNow;
            job.UpdatedOnUtc = DateTime.UtcNow;
            await _unitOfWork.SaveChangesAsync(ct);
            return;
        }

        // 4. Load player progress
        var progress = await _unitOfWork.Repository<HrkPlayerEventPeriodProgress>().Query()
            .FirstOrDefaultAsync(p => p.PlayerId == job.PlayerId && p.EventPeriodId == job.EventPeriodId, ct);
        if (progress == null)
        {
            job.Status = "ERROR";
            job.StopReason = "ERROR";
            job.CompletedOnUtc = DateTime.UtcNow;
            await _unitOfWork.SaveChangesAsync(ct);
            return;
        }

        int currentFloor = job.CurrentFloor;
        // 5. Load floor & enemies
        var floor = await _unitOfWork.ReadOnlyRepository<HrkTowerFloor>().Query()
            .Where(f => f.EventId == progress.EventId && f.FloorNumber == currentFloor && f.IsActive)
            .Include(f => f.Enemies)
            .FirstOrDefaultAsync(ct);
        if (floor == null)
        {
            job.Status = "ERROR";
            job.StopReason = "ERROR";
            job.CompletedOnUtc = DateTime.UtcNow;
            await _unitOfWork.SaveChangesAsync(ct);
            return;
        }

        // 6. Deserialize frozen formation snapshot
        var snapshot = JsonSerializer.Deserialize<FormationBattleSnapshot>(job.FormationSnapshotJson) ?? throw new InvalidOperationException("Không thể đọc snapshot đội hình của phiên leo nhanh.");
        var player = await _gamePlayerService.GetPlayerByIdAsync(job.PlayerId, ct, tracked: true) ?? throw new InvalidOperationException("Không tìm thấy người chơi của phiên leo nhanh.");
        var allTime = await _unitOfWork.Repository<HrkPlayerEventAllTimeRecord>().Query()
            .SingleAsync(a => a.PlayerId == job.PlayerId && a.EventId == progress.EventId, ct);
        // Execute battle in its own transaction
        {
            var (initialState, simulation, randomSeed) = await _executor.SimulateTowerFloorBattle(player, snapshot, floor, ct);
            var heroStats = BattleStatisticsCalculator.Calculate(initialState, simulation.Events);
            bool isVictory = simulation.Winner.Equals("LEFT", StringComparison.OrdinalIgnoreCase);
            string battleId = Guid.NewGuid().ToString("N");
            job.LastBattleId = battleId;
            var accumulatedRewards = ParseRewardsJson(job.AccumulatedRewardsJson);
            var logs = ParseLogsJson(job.LogsJson);
            var floorRewardsGained = new List<GenericRewardItemDto>();
            var pendingRewards = new List<GenericRewardItemDto>();
            var heroExpResults = new List<HeroExpResultDto>();
            int playerExp = 0;
            int livesBefore = job.RemainingLives;
            if (isVictory)
            {
                progress.HighestFloorInPeriod = Math.Max(progress.HighestFloorInPeriod, currentFloor);
                allTime.HighestFloorAllTime = Math.Max(allTime.HighestFloorAllTime, currentFloor);
                job.ClearedFloorsCount++;
                // Quick-climb is a repeatable daily run. Every cleared floor grants its configured
                // reward again; milestone chests remain once per event period.
                var configuredRewards = ParseRewardsJson(floor.RewardsJson);
                var (granted, pending, _) = await _rewards.GrantGenericRewardsAsync(player.Id, configuredRewards, period.Id, $"Leo nhanh lượt {job.DailyRunNumber} - Tầng {currentFloor}", ct, experienceHandled: true);
                floorRewardsGained.AddRange(granted);
                pendingRewards.AddRange(pending);
                var experience = TowerRewardService.GetExperience(configuredRewards);
                playerExp = experience.Player;
                await _rewards.AddPlayerExpAsync(player, playerExp, ct);
                heroExpResults = await _rewards.AddParticipantHeroExp(player.Id, snapshot.ParticipantHeroIds, experience.Hero, ct);
                MergeRewards(accumulatedRewards, granted);
                var expRewards = configuredRewards.Where(r => r.Type is "PLAYER_EXP" or "HERO_EXP").ToList();
                MergeRewards(accumulatedRewards, expRewards);
                floorRewardsGained.AddRange(expRewards);
                if (currentFloor >= job.TargetFloor)
                {
                    // Tower completed!
                    progress.CurrentFloor = job.TargetFloor;
                    progress.IsCompleted = true;
                    allTime.TotalClears++;
                    job.Status = "COMPLETED";
                    job.StopReason = "TOWER_COMPLETED";
                    job.CompletedOnUtc = DateTime.UtcNow;
                }
                else
                {
                    progress.CurrentFloor = Math.Max(progress.CurrentFloor, currentFloor + 1);
                    job.CurrentFloor = currentFloor + 1;
                    if (job.StopReason == "USER_CANCELLED")
                    {
                        job.Status = "CANCELLED";
                        job.CompletedOnUtc = DateTime.UtcNow;
                    }
                }
            }
            else
            {
                // FIRST DEFEAT: Stop quick climb immediately!
                job.RemainingLives = 0;
                job.FailedFloor = currentFloor;
                job.Status = "STOPPED_DEFEAT";
                job.StopReason = "FIRST_DEFEAT";
                job.CompletedOnUtc = DateTime.UtcNow;
            }

            // Add log entry
            logs.Add(new QuickClimbFloorLogDto { FloorNumber = currentFloor, FloorName = floor.Name, IsVictory = isVictory, TotalTurns = simulation.Events.Select(e => e.Round).DefaultIfEmpty(0).Max(), LivesRemaining = job.RemainingLives, RewardsGained = floorRewardsGained, TimestampUtc = DateTime.UtcNow });
            job.AccumulatedRewardsJson = JsonSerializer.Serialize(accumulatedRewards);
            job.LogsJson = JsonSerializer.Serialize(logs);
            job.UpdatedOnUtc = DateTime.UtcNow;
            progress.UpdatedOn = DateTime.UtcNow;
            allTime.UpdatedOn = DateTime.UtcNow;
            // Save battle record for review
            var battleRecord = new HrkPlayerTowerBattle
            {
                BattleId = battleId,
                PlayerId = player.Id,
                EventPeriodId = period.Id,
                FloorNumber = currentFloor,
                QuickClimbJobId = job.Id,
                Result = isVictory ? "VICTORY" : "DEFEAT",
                LivesBefore = livesBefore,
                LivesAfter = job.RemainingLives,
                PlayerPower = snapshot.TotalPower,
                EnemyPower = initialState.RightTeam.Sum(e => e.Power),
                RandomSeed = randomSeed,
                HeroStatisticsJson = JsonSerializer.Serialize(heroStats),
                CreatedOnUtc = DateTime.UtcNow
            };
            _activityEvents.RaiseEvent(player, period, "TowerBattleCompleted", battleRecord.BattleId,
                new { battleRecord.BattleId, battleRecord.FloorNumber, battleRecord.Result, battleRecord.LivesBefore,
                    battleRecord.LivesAfter, QuickClimbJobId = job.JobId }, battleRecord.CreatedOnUtc);
            var result = new StartTowerBattleResultDto
            {
                Battle = TowerBattleExecutor.ToBattleResult(battleId, initialState, simulation, randomSeed, heroStats),
                FloorNumber = currentFloor,
                NextFloorNumber = job.Status is "QUEUED" or "PROCESSING" ? job.CurrentFloor : null,
                LivesBefore = livesBefore,
                LivesAfter = job.RemainingLives,
                IsVictory = isVictory,
                IsRunEnded = job.Status is not ("QUEUED" or "PROCESSING"),
                IsTowerCompleted = job.Status == "COMPLETED",
                EarnedRewards = floorRewardsGained,
                PendingRewards = pendingRewards,
                PlayerExpGained = playerExp,
                HeroExpGained = heroExpResults.FirstOrDefault()?.ExpGained ?? 0,
                NewPlayerLevel = player.Level,
                NewPlayerExp = player.Exp,
                Heroes = heroExpResults
            };
            battleRecord.ResultJson = JsonSerializer.Serialize(result);
            await _unitOfWork.Repository<HrkPlayerTowerBattle>().AddAsync(battleRecord);
            await _unitOfWork.SaveChangesAsync(ct);
        }
    }

    private static void MergeRewards(List<GenericRewardItemDto> target, List<GenericRewardItemDto> source)
    {
        foreach (var s in source)
        {
            var existing = target.FirstOrDefault(t => t.Type == s.Type && t.Code == s.Code && t.ItemTemplateId == s.ItemTemplateId && t.StoneGrade == s.StoneGrade && t.CharmType == s.CharmType);
            if (existing != null)
            {
                existing.Quantity += s.Quantity;
            }
            else
            {
                target.Add(new GenericRewardItemDto { Type = s.Type, Code = s.Code, ItemTemplateId = s.ItemTemplateId, Name = s.Name, ImagePath = s.ImagePath, RarityCode = s.RarityCode, RarityColorHex = s.RarityColorHex, CategoryName = s.CategoryName, Quantity = s.Quantity, StoneGrade = s.StoneGrade, CharmType = s.CharmType, Description = s.Description });
            }
        }
    }

    private static bool IsTransientSql(SqlException exception) => exception.Errors.Cast<SqlError>().Any(error => error.Number is -2 or 64 or 233 or 1205 or 4060 or 10928 or 10929 or 40197 or 40501 or 40613 or 49918 or 49919 or 49920 or 10053 or 10054 or 10060 or 51000);
}
