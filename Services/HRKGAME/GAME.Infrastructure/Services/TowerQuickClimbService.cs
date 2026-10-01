using Core.Common.Repositories;
using Microsoft.Data.SqlClient;
using GAME.Application.Common;
using GAME.Application.Common.Helpers;
using GAME.Application.Common.Mappings;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;
using GAME.Domain.Battle;
using GAME.Domain.Entities;
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

    public TowerQuickClimbService(
        TowerOperationRunner runner, TowerBattleExecutor executor, TowerRewardService rewards,
        IUnitOfWork unitOfWork,
        IEventPeriodService eventPeriodService,
        IFormationSnapshotService formationSnapshotService,
        ILogger<TowerQuickClimbService> logger)
    {
        _runner = runner;
        _executor = executor;
        _rewards = rewards;
        _unitOfWork = unitOfWork;
        _eventPeriodService = eventPeriodService;
        _formationSnapshotService = formationSnapshotService;
        _logger = logger;
    }

    public Task<QuickClimbJobStatusDto> StartQuickClimbAsync(
        string userId, StartQuickClimbRequestDto request, CancellationToken ct = default) =>
        _runner.RunAsync(userId, () => StartQuickClimbAsyncCore(userId, request, ct), ct);

    private async Task<QuickClimbJobStatusDto> StartQuickClimbAsyncCore(
        string userId, StartQuickClimbRequestDto request, CancellationToken ct = default)
    {
        var player = await GetPlayerAsync(userId, ct);
        var ev = await GetTowerEventAsync(ct);
        EventAccessPolicy.EnsureCanEnter(ev, player.Level, DateTime.UtcNow);

        var currentPeriod = await _eventPeriodService.GetOrCreateCurrentPeriodAsync(ev, ct);
        var progress = await _eventPeriodService.GetOrCreatePlayerPeriodProgressAsync(player.Id, ev, currentPeriod, ct);

        if (progress.RemainingLives <= 0)
            throw new InvalidOperationException("Đã hết mạng trong lượt leo hiện tại. Vui lòng bắt đầu lượt mới trước khi leo nhanh.");
        if (progress.IsCompleted)
            throw new InvalidOperationException("Đã hoàn thành toàn bộ tầng tháp trong kỳ này.");

        // Check if player already has an active job
        var existingJob = await _unitOfWork.Repository<HrkTowerQuickClimbJob>().Query()
            .FirstOrDefaultAsync(j => j.PlayerId == player.Id && (j.Status == "QUEUED" || j.Status == "PROCESSING"), ct);

        if (existingJob != null)
        {
            // Resume existing job
            return MapJobToDto(existingJob);
        }

        // Build frozen snapshot
        var snapshot = await _formationSnapshotService.BuildAsync(player.Id, request.FormationCode, request.Positions, ct);
        if (snapshot.Heroes.Count == 0)
            throw new InvalidOperationException("Đội hình xuất chiến không có võ tướng nào.");

        await _executor.FreezeSkillsAsync(snapshot, ct, force: true);
        var job = new HrkTowerQuickClimbJob
        {
            JobId = Guid.NewGuid().ToString("N"),
            PlayerId = player.Id,
            EventPeriodId = currentPeriod.Id,
            Status = "QUEUED",
            StartFloor = progress.CurrentFloor,
            CurrentFloor = progress.CurrentFloor,
            TargetFloor = await _executor.MaxFloorAsync(ev.Id, ct),
            InitialLives = progress.RemainingLives,
            RemainingLives = progress.RemainingLives,
            ClearedFloorsCount = 0,
            FormationCode = snapshot.FormationCode,
            FormationSnapshotJson = JsonSerializer.Serialize(snapshot),
            AccumulatedRewardsJson = "[]",
            LogsJson = "[]",
            CreatedOnUtc = DateTime.UtcNow,
            UpdatedOnUtc = DateTime.UtcNow
        };

        await _unitOfWork.Repository<HrkTowerQuickClimbJob>().AddAsync(job);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Started quick climb job {JobId} for Player {PlayerId} from floor {StartFloor}",
            job.JobId, player.Id, job.StartFloor);

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

    public async Task<bool> ProcessNextJobFloorAsync(string workerId, CancellationToken ct = default)
    {
        var candidate = await _unitOfWork.ReadOnlyRepository<HrkTowerQuickClimbJob>().Query()
            .Where(j => j.Status == "QUEUED" || j.Status == "PROCESSING")
            .OrderBy(j => j.UpdatedOnUtc).FirstOrDefaultAsync(ct);
        if (candidate == null) return false;
        var userId = await _unitOfWork.ReadOnlyRepository<HrkPlayer>().Query()
            .Where(p => p.Id == candidate.PlayerId).Select(p => p.UserId).SingleAsync(ct);
        string jobId = candidate.JobId;
        int expectedFloor = candidate.CurrentFloor;
        try
        {
            return await _runner.RunAsync(userId, async () =>
            {
                var job = await _unitOfWork.Repository<HrkTowerQuickClimbJob>().Query()
                    .SingleAsync(j => j.JobId == jobId, ct);
                // Another worker (or a retried commit) already advanced this attempt.
                if (job.CurrentFloor != expectedFloor || (job.Status != "QUEUED" && job.Status != "PROCESSING"))
                    return false;
                job.Status = "PROCESSING";
                job.WorkerId = workerId;
                job.LockedUntilUtc = null; // Transaction-owned DB lock, released immediately after this floor.
                await ProcessSingleFloorIterationAsync(job, ct);
                return true;
            }, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (SqlException ex) when (ex.Number == 51000)
        {
            return false; // Lock contention is not a battle/system failure.
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Quick climb {JobId} failed at floor {Floor}", jobId, expectedFloor);
            await _runner.RunAsync(userId, async () =>
            {
                var job = await _unitOfWork.Repository<HrkTowerQuickClimbJob>().Query()
                    .SingleAsync(j => j.JobId == jobId, ct);
                if (job.CurrentFloor == expectedFloor && (job.Status == "QUEUED" || job.Status == "PROCESSING"))
                {
                    job.Status = "ERROR";
                    job.StopReason = "ERROR";
                    job.CompletedOnUtc = DateTime.UtcNow;
                }
                return true;
            }, ct);
            return true;
        }
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

                // Grant floor rewards if not already claimed in period
                bool alreadyClaimed = await _unitOfWork.ReadOnlyRepository<HrkPlayerEventRewardClaim>().Query()
                    .AnyAsync(c => c.PlayerId == player.Id && c.EventPeriodId == period.Id && c.ClaimType == "FLOOR_REWARD" && c.TargetId == currentFloor, ct);

                if (!alreadyClaimed)
                {
                    var configuredRewards = ParseRewardsJson(floor.RewardsJson);
                    var (granted, pending, bagFull) = await _rewards.GrantGenericRewardsAsync(player.Id, configuredRewards, period.Id, $"Leo nhanh Tầng {currentFloor}", ct, experienceHandled: true);
                    floorRewardsGained.AddRange(granted);
                    pendingRewards.AddRange(pending);
                    var experience = TowerRewardService.GetExperience(configuredRewards);
                    playerExp = experience.Player;
                    await _rewards.AddPlayerExpAsync(player, playerExp, ct);
                    heroExpResults = await _rewards.AddParticipantHeroExp(player.Id, snapshot.ParticipantHeroIds, experience.Hero, ct);

                    // Merge into accumulated rewards
                    MergeRewards(accumulatedRewards, granted);
                    var expRewards = configuredRewards.Where(r => r.Type is "PLAYER_EXP" or "HERO_EXP").ToList();
                    MergeRewards(accumulatedRewards, expRewards);
                    floorRewardsGained.AddRange(expRewards);

                    var claim = new HrkPlayerEventRewardClaim
                    {
                        PlayerId = player.Id,
                        EventPeriodId = period.Id,
                        ClaimType = "FLOOR_REWARD",
                        TargetId = currentFloor,
                        ClaimedAtUtc = DateTime.UtcNow,
                        RewardSummaryJson = JsonSerializer.Serialize(granted)
                    };
                    await _unitOfWork.Repository<HrkPlayerEventRewardClaim>().AddAsync(claim);
                }

                if (currentFloor >= job.TargetFloor)
                {
                    // Tower completed!
                    progress.IsCompleted = true;
                    allTime.TotalClears++;
                    job.Status = "COMPLETED";
                    job.StopReason = "TOWER_COMPLETED";
                    job.CompletedOnUtc = DateTime.UtcNow;
                }
                else
                {
                    progress.CurrentFloor = currentFloor + 1;
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
                job.RemainingLives = EventAccessPolicy.LivesAfter(ev, job.RemainingLives, false);
                progress.RemainingLives = job.RemainingLives;
                job.FailedFloor = currentFloor;

                if (job.RemainingLives <= 0)
                {
                    // Out of lives: reset run to floor 1
                    progress.CurrentFloor = 1;
                }

                job.Status = "STOPPED_DEFEAT";
                job.StopReason = "FIRST_DEFEAT";
                job.CompletedOnUtc = DateTime.UtcNow;
            }

            if (isVictory && ev.LifeConsumeMode == "ON_ENTRY")
            {
                job.RemainingLives = EventAccessPolicy.LivesAfter(ev, job.RemainingLives, true);
                progress.RemainingLives = job.RemainingLives;
                if (job.RemainingLives == 0 && !progress.IsCompleted)
                {
                    progress.CurrentFloor = 1;
                    job.Status = "STOPPED_DEFEAT";
                    job.StopReason = "LIVES_EXHAUSTED";
                    job.CompletedOnUtc = DateTime.UtcNow;
                }
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
                NextFloorNumber = progress.IsCompleted || progress.RemainingLives == 0 ? null : progress.CurrentFloor,
                LivesBefore = livesBefore,
                LivesAfter = job.RemainingLives,
                IsVictory = isVictory,
                IsRunEnded = progress.RemainingLives == 0,
                IsTowerCompleted = progress.IsCompleted,
                EarnedRewards = floorRewardsGained,
                PendingRewards = pendingRewards,
                PlayerExpGained = playerExp,
                HeroExpGained = heroExpResults.FirstOrDefault()?.ExpGained ?? 0,
                NewPlayerLevel = player.Level,
                NewPlayerExp = player.Exp,
                Heroes = heroExpResults
            };
            battleRecord.ResultJson = JsonSerializer.Serialize(result);
            await _executor.UpdateRunAsync(progress, currentFloor, ct);
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
        FailedFloor = job.FailedFloor,
        AccumulatedRewards = ParseRewardsJson(job.AccumulatedRewardsJson),
        Logs = ParseLogsJson(job.LogsJson),
        LastBattleId = job.LastBattleId,
        CreatedOnUtc = job.CreatedOnUtc,
        UpdatedOnUtc = job.UpdatedOnUtc,
        CompletedOnUtc = job.CompletedOnUtc
    };

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
