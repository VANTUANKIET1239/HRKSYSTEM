using Core.Common.Repositories;
using Core.Messaging.Contracts;
using Core.TransactionalMessaging.Inbox;
using Core.TransactionalMessaging.Outbox;
using Microsoft.Data.SqlClient;
using GAME.Application.Common;
using GAME.Application.Common.Helpers;
using GAME.Application.Common.Mappings;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;
using GAME.Application.Events;
using GAME.Domain.Battle;
using GAME.Domain.Entities;
using GAME.Infrastructure.Messaging.Consumers;
using Oservability.Tracing;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace GAME.Infrastructure.Services;

public class TowerQuickClimbService : ITowerQuickClimbService
{
    private readonly TowerOperationRunner _runner;
    private readonly TowerBattleExecutor _executor;
    private readonly TowerRewardService _rewards;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEventPeriodService _eventPeriodService;
    private readonly IFormationSnapshotService _formationSnapshotService;
    private readonly ILogger<TowerQuickClimbService> _logger;
    private readonly IInboxExecutor _inbox;
    private readonly IOutboxWriter _outbox;
    private readonly IPlayerActivityEvents _activityEvents;

    public TowerQuickClimbService(
        TowerOperationRunner runner, TowerBattleExecutor executor, TowerRewardService rewards,
        IUnitOfWork unitOfWork,
        IEventPeriodService eventPeriodService,
        IFormationSnapshotService formationSnapshotService,
        IInboxExecutor inbox,
        IOutboxWriter outbox,
        ILogger<TowerQuickClimbService> logger,
        IPlayerActivityEvents activityEvents)
    {
        _runner = runner;
        _executor = executor;
        _rewards = rewards;
        _unitOfWork = unitOfWork;
        _eventPeriodService = eventPeriodService;
        _formationSnapshotService = formationSnapshotService;
        _inbox = inbox;
        _outbox = outbox;
        _logger = logger;
        _activityEvents = activityEvents;
    }

    public async Task<QuickClimbJobStatusDto> StartQuickClimbAsync(
        string userId, StartQuickClimbRequestDto request, CancellationToken ct = default)
    {
        long? createdPlayerId = null;
        var result = await _runner.RunAsync(userId, () =>
        {
            createdPlayerId = null;
            return StartQuickClimbAsyncCore(userId, request, id => createdPlayerId = id, ct);
        }, ct);
        _logger.Log(createdPlayerId.HasValue ? LogLevel.Information : LogLevel.Debug,
            "{EventName}: JobId={JobId}, UserId={UserId}, PlayerId={PlayerId}, Status={Status}, TargetFloor={TargetFloor}, DailyRunNumber={DailyRunNumber}",
            createdPlayerId.HasValue ? "QuickClimbJobCreated" : "QuickClimbJobReused",
            result.JobId, userId, createdPlayerId, result.Status, result.TargetFloor, result.DailyRunNumber);
        return result;
    }

    private async Task<QuickClimbJobStatusDto> StartQuickClimbAsyncCore(
        string userId, StartQuickClimbRequestDto request, Action<long> onCreated, CancellationToken ct = default)
    {
        var player = await GetPlayerAsync(userId, ct);
        var ev = await GetTowerEventAsync(ct);
        EventAccessPolicy.EnsureCanEnter(ev, player.Level, DateTime.UtcNow);

        var currentPeriod = await _eventPeriodService.GetOrCreateCurrentPeriodAsync(ev, ct);
        var progress = await _eventPeriodService.GetOrCreatePlayerPeriodProgressAsync(player.Id, ev, currentPeriod, ct);

        // Check if player already has an active job
        var existingJob = await _unitOfWork.Repository<HrkTowerQuickClimbJob>().Query()
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
            throw new InvalidOperationException(
                $"Đã sử dụng hết {rules.QuickClimbDailyLimit} lượt leo nhanh trong kỳ hôm nay.");
        }

        // Build frozen snapshot
        var snapshot = await _formationSnapshotService.BuildAsync(player.Id, request.FormationCode, request.Positions, ct);
        if (snapshot.Heroes.Count == 0)
            throw new InvalidOperationException("Đội hình xuất chiến không có võ tướng nào.");

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
        _activityEvents.Raise(new QuickClimbStartedEvent(player.Id, userId, job.JobId,
            job.StartFloor, job.CurrentFloor, job.TargetFloor, job.ClearedFloorsCount, job.Version));
        EnqueueStatus(job, userId);
        EnqueueFloorCommand(job, userId);
        await _unitOfWork.SaveChangesAsync(ct);

        onCreated(player.Id);

        return MapJobToDto(job);
    }

    public Task<QuickClimbJobStatusDto?> GetActiveJobAsync(string userId, CancellationToken ct = default) =>
        _runner.RunAsync(userId, () => GetActiveJobAsyncCore(userId, ct), ct);

    private async Task<QuickClimbJobStatusDto?> GetActiveJobAsyncCore(string userId, CancellationToken ct = default)
    {
        var player = await GetPlayerAsync(userId, ct);
        var job = await _unitOfWork.ReadOnlyRepository<HrkTowerQuickClimbJob>().Query()
            .Where(j => j.PlayerId == player.Id)
            .OrderByDescending(j => j.CreatedOnUtc)
            .FirstOrDefaultAsync(ct);

        if (job == null) return null;

        // If job is still active, or completed very recently (within 10 minutes)
        if (job.Status == "QUEUED" || job.Status == "PROCESSING" ||
            (job.CompletedOnUtc.HasValue && (DateTime.UtcNow - job.CompletedOnUtc.Value).TotalMinutes < 10))
        {
            return MapJobToDto(job);
        }

        return null;
    }

    public Task<QuickClimbJobStatusDto> GetJobStatusAsync(string userId, string jobId, CancellationToken ct = default) =>
        _runner.RunAsync(userId, () => GetJobStatusAsyncCore(userId, jobId, ct), ct);

    private async Task<QuickClimbJobStatusDto> GetJobStatusAsyncCore(string userId, string jobId, CancellationToken ct = default)
    {
        var player = await GetPlayerAsync(userId, ct);
        var job = await _unitOfWork.ReadOnlyRepository<HrkTowerQuickClimbJob>().Query()
            .FirstOrDefaultAsync(j => j.JobId == jobId && j.PlayerId == player.Id, ct)
            ?? throw new KeyNotFoundException($"Không tìm thấy phiên leo nhanh '{jobId}'.");

        return MapJobToDto(job);
    }

    public Task<QuickClimbJobStatusDto> StopQuickClimbAsync(string userId, string jobId, CancellationToken ct = default) =>
        _runner.RunAsync(userId, () => StopQuickClimbAsyncCore(userId, jobId, ct), ct);

    private async Task<QuickClimbJobStatusDto> StopQuickClimbAsyncCore(string userId, string jobId, CancellationToken ct = default)
    {
        var player = await GetPlayerAsync(userId, ct);
        var job = await _unitOfWork.Repository<HrkTowerQuickClimbJob>().Query()
            .FirstOrDefaultAsync(j => j.JobId == jobId && j.PlayerId == player.Id, ct)
            ?? throw new KeyNotFoundException($"Không tìm thấy phiên leo nhanh '{jobId}'.");

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

    public async Task<bool> ProcessJobFloorMessageAsync(
        Guid messageId,
        string jobId,
        string userId,
        int expectedFloor,
        long expectedVersion,
        string workerId,
        CancellationToken ct = default)
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
                var execution = await _inbox.ExecuteInCurrentTransactionAsync(
                    consumerName,
                    messageKey,
                    async token =>
                    {
                        var job = await _unitOfWork.Repository<HrkTowerQuickClimbJob>().Query()
                            .SingleAsync(x => x.JobId == jobId, token);
                        if (job.CurrentFloor != expectedFloor ||
                            job.Version != expectedVersion ||
                            (job.Status != "QUEUED" && job.Status != "PROCESSING"))
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
                    },
                    ct);

                return !execution.IsDuplicate && execution.Result;
            }, ct);
            if (processed && processedJob is not null)
            {
                activity?.SetTag("quickclimb.status", processedJob.Status);
                var floorLog = ParseLogsJson(processedJob.LogsJson)
                    .LastOrDefault(x => x.FloorNumber == expectedFloor);
                activity?.SetTag("quickclimb.floor.result", floorLog is null ? "NOT_RUN"
                    : floorLog.IsVictory ? "VICTORY" : "DEFEAT");
                if (processedJob.Status == "ERROR") activity?.SetStatus(ActivityStatusCode.Error);
                if (floorLog is not null)
                    _logger.LogDebug(
                        "QuickClimbFloorProcessed: JobId={JobId}, PlayerId={PlayerId}, Floor={Floor}, Result={Result}, TotalTurns={TotalTurns}, DurationMs={DurationMs}, MessageId={MessageId}",
                        jobId, processedJob.PlayerId, expectedFloor,
                        floorLog.IsVictory ? "VICTORY" : "DEFEAT", floorLog.TotalTurns,
                        timer.Elapsed.TotalMilliseconds, messageId);
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
            throw new QuickClimbTransientMessageException(
                "Quick-climb persistence is temporarily unavailable.",
                exception);
        }
        catch (TimeoutException exception)
        {
            activity?.SetStatus(ActivityStatusCode.Error, exception.GetType().Name);
            throw new QuickClimbTransientMessageException(
                "Quick-climb processing timed out.",
                exception);
        }
        catch (Exception exception)
        {
            activity?.SetStatus(ActivityStatusCode.Error, exception.GetType().Name);
            _logger.LogError(
                exception,
                "Quick climb message {MessageId} failed for job {JobId} at floor {Floor}.",
                messageId,
                jobId,
                expectedFloor);

            HrkTowerQuickClimbJob? failedJob = null;
            var failureRecorded = await _runner.RunAsync(userId, async () =>
            {
                failedJob = null;
                var execution = await _inbox.ExecuteInCurrentTransactionAsync(
                    consumerName,
                    messageKey,
                    async token =>
                    {
                        var job = await _unitOfWork.Repository<HrkTowerQuickClimbJob>().Query()
                            .SingleAsync(x => x.JobId == jobId, token);
                        if (job.CurrentFloor == expectedFloor &&
                            job.Version == expectedVersion &&
                            job.Status is "QUEUED" or "PROCESSING")
                        {
                            job.Status = "ERROR";
                            job.StopReason = "ERROR";
                            job.CompletedOnUtc = DateTime.UtcNow;
                            job.UpdatedOnUtc = DateTime.UtcNow;
                            job.Version++;
                            failedJob = job;
                            EnqueueStatus(
                                job,
                                userId,
                                "QUICK_CLIMB_FAILED",
                                "Quick climb could not continue because of a system error.");
                        }

                        return true;
                    },
                    ct);

                return !execution.IsDuplicate && execution.Result;
            }, ct);
            if (failureRecorded && failedJob is not null)
                LogCommittedJobEnd(failedJob, messageId);
            return true;
        }
    }

    private void LogCommittedJobEnd(HrkTowerQuickClimbJob job, Guid messageId)
    {
        if (job.Status is "QUEUED" or "PROCESSING") return;
        _logger.LogInformation(
            "QuickClimbEnded: JobId={JobId}, PlayerId={PlayerId}, Status={Status}, ClearedFloorsCount={ClearedFloorsCount}, TargetFloor={TargetFloor}, StopReason={StopReason}, MessageId={MessageId}",
            job.JobId, job.PlayerId, job.Status, job.ClearedFloorsCount,
            job.TargetFloor, job.StopReason, messageId);
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
            .Include(f => f.Enemies).ThenInclude(e => e.HeroTemplate)
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
        var snapshot = JsonSerializer.Deserialize<FormationBattleSnapshot>(job.FormationSnapshotJson)
            ?? throw new InvalidOperationException("Không thể đọc snapshot đội hình của phiên leo nhanh.");

        var player = await _unitOfWork.Repository<HrkPlayer>().Query()
            .SingleAsync(p => p.Id == job.PlayerId, ct);
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
                var (granted, pending, _) = await _rewards.GrantGenericRewardsAsync(
                    player.Id,
                    configuredRewards,
                    period.Id,
                    $"Leo nhanh lượt {job.DailyRunNumber} - Tầng {currentFloor}",
                    ct,
                    experienceHandled: true);
                floorRewardsGained.AddRange(granted);
                pendingRewards.AddRange(pending);
                var experience = TowerRewardService.GetExperience(configuredRewards);
                playerExp = experience.Player;
                await _rewards.AddPlayerExpAsync(player, playerExp, ct);
                heroExpResults = await _rewards.AddParticipantHeroExp(
                    player.Id,
                    snapshot.ParticipantHeroIds,
                    experience.Hero,
                    ct);

                MergeRewards(accumulatedRewards, granted);
                var expRewards = configuredRewards
                    .Where(r => r.Type is "PLAYER_EXP" or "HERO_EXP")
                    .ToList();
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
            logs.Add(new QuickClimbFloorLogDto
            {
                FloorNumber = currentFloor,
                FloorName = floor.Name,
                IsVictory = isVictory,
                TotalTurns = simulation.Events.Select(e => e.Round).DefaultIfEmpty(0).Max(),
                LivesRemaining = job.RemainingLives,
                RewardsGained = floorRewardsGained,
                TimestampUtc = DateTime.UtcNow
            });

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
                target.Add(new GenericRewardItemDto
                {
                    Type = s.Type,
                    Code = s.Code,
                    ItemTemplateId = s.ItemTemplateId,
                    Name = s.Name,
                    ImagePath = s.ImagePath,
                    RarityCode = s.RarityCode,
                    RarityColorHex = s.RarityColorHex,
                    CategoryName = s.CategoryName,
                    Quantity = s.Quantity,
                    StoneGrade = s.StoneGrade,
                    CharmType = s.CharmType,
                    Description = s.Description
                });
            }
        }
    }

    private static QuickClimbJobStatusDto MapJobToDto(HrkTowerQuickClimbJob job) => new()
    {
        JobId = job.JobId,
        Status = job.Status,
        StopReason = job.StopReason,
        StartFloor = job.StartFloor,
        CurrentFloor = job.CurrentFloor,
        TargetFloor = job.TargetFloor,
        InitialLives = job.InitialLives,
        RemainingLives = job.RemainingLives,
        ClearedFloorsCount = job.ClearedFloorsCount,
        DailyRunNumber = job.DailyRunNumber,
        FailedFloor = job.FailedFloor,
        AccumulatedRewards = ParseRewardsJson(job.AccumulatedRewardsJson),
        Logs = ParseLogsJson(job.LogsJson),
        LastBattleId = job.LastBattleId,
        CreatedOnUtc = job.CreatedOnUtc,
        UpdatedOnUtc = job.UpdatedOnUtc,
        CompletedOnUtc = job.CompletedOnUtc,
        Version = job.Version
    };

    private static bool IsTransientSql(SqlException exception) =>
        exception.Errors.Cast<SqlError>().Any(error => error.Number is
            -2 or 64 or 233 or 1205 or 4060 or 10928 or 10929 or 40197 or 40501 or 40613 or
            49918 or 49919 or 49920 or 10053 or 10054 or 10060 or 51000);

    private void EnqueueFloorCommand(HrkTowerQuickClimbJob job, string userId)
    {
        var command = new ProcessQuickClimbFloorRequestedV1(
            Guid.NewGuid(),
            job.JobId,
            userId,
            job.CurrentFloor,
            job.Version);
        _outbox.Add(
            command,
            MessagingContractNames.QuickClimbFloorRequestedV1,
            MessagingPublisherNames.GameEvents,
            MessagingContractNames.QuickClimbFloorRequestedV1,
            partitionKey: job.JobId,
            sequence: job.Version);
    }

    private void EnqueueStatus(
        HrkTowerQuickClimbJob job,
        string userId,
        string? errorCode = null,
        string? errorMessage = null)
    {
        var total = Math.Max(1, job.TargetFloor - job.StartFloor + 1);
        var percentage = Math.Clamp((int)Math.Round(job.ClearedFloorsCount * 100d / total), 0, 100);
        if (job.Status == "COMPLETED")
        {
            percentage = 100;
        }

        var statusEvent = new ProcessStatusUpdatedV1(
            Guid.NewGuid(),
            job.JobId,
            userId,
            "TOWER_QUICK_CLIMB",
            job.Version,
            job.CurrentFloor,
            job.TargetFloor,
            percentage,
            job.Status,
            DateTime.UtcNow,
            errorCode,
            errorMessage is null ? null : errorMessage[..Math.Min(errorMessage.Length, 500)]);
        _outbox.Add(
            statusEvent,
            MessagingContractNames.ProcessStatusUpdatedV1,
            MessagingPublisherNames.GameEvents,
            MessagingContractNames.ProcessStatusUpdatedV1,
            partitionKey: job.JobId,
            sequence: job.Version);
        if (job.Status is not ("QUEUED" or "PROCESSING"))
            _activityEvents.Raise(new QuickClimbEndedEvent(job.PlayerId, userId, job.JobId,
                job.Status, job.StartFloor, job.CurrentFloor, job.TargetFloor,
                job.ClearedFloorsCount, job.StopReason, job.Version));
    }

    private static List<GenericRewardItemDto> ParseRewardsJson(string? json) =>
        TowerRewardService.ParseRewards(json);

    private static List<QuickClimbFloorLogDto> ParseLogsJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new();
        try
        {
            return JsonSerializer.Deserialize<List<QuickClimbFloorLogDto>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
        }
        catch
        {
            return new();
        }
    }

    private async Task<HrkPlayer> GetPlayerAsync(string userId, CancellationToken ct) =>
        await _unitOfWork.Repository<HrkPlayer>().Query().SingleOrDefaultAsync(x => x.UserId == userId && x.IsActive, ct)
        ?? throw new KeyNotFoundException("Không tìm thấy người chơi.");

    private async Task<HrkGameEvent> GetTowerEventAsync(CancellationToken ct) =>
        await _unitOfWork.ReadOnlyRepository<HrkGameEvent>().Query()
            .FirstOrDefaultAsync(e => e.Code == "TOWER_CLIMB", ct)
            ?? throw new KeyNotFoundException("Không tìm thấy cấu hình sự kiện 'TOWER_CLIMB'.");
}
