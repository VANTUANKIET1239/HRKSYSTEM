using Core.Persistence.Repositories;
using Microsoft.Data.SqlClient;
using GAME.Application.Interfaces;
using GAME.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using GAME.Application.Common;
using GAME.Application.Events;
using Core.Messaging.Contracts;
using Core.TransactionalMessaging.Outbox;
using System.Text.Json;

namespace GAME.Application.Services;

public sealed class TowerEventService : ITowerEventService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPlayerActivityEvents _activities;
    private readonly IOutboxWriter _outbox;
    private readonly IEventResetSchedule _schedule;

    public TowerEventService(IUnitOfWork unitOfWork, IPlayerActivityEvents activities,
        IOutboxWriter outbox, IEventResetSchedule schedule)
    {
        _unitOfWork = unitOfWork;
        _activities = activities;
        _outbox = outbox;
        _schedule = schedule;
    }

    public async Task<HrkEventPeriod> ReadCurrentPeriodAsync(HrkGameEvent gameEvent, CancellationToken ct = default)
    {
        var (start, end, key) = await GetCurrentBoundariesAsync(gameEvent, ct);
        return await _unitOfWork.ReadOnlyRepository<HrkEventPeriod>().Query()
            .FirstOrDefaultAsync(x => x.EventId == gameEvent.Id && x.PeriodKey == key, ct) ?? new HrkEventPeriod
            {
                EventId = gameEvent.Id,
                PeriodKey = key,
                StartAtUtc = start,
                EndAtUtc = end
            };
    }

    public async Task<HrkPlayerEventPeriodProgress> ReadPlayerPeriodProgressAsync(long playerId, HrkGameEvent gameEvent,
        HrkEventPeriod currentPeriod, CancellationToken ct = default) =>
        await _unitOfWork.ReadOnlyRepository<HrkPlayerEventPeriodProgress>().Query()
            .FirstOrDefaultAsync(x => x.PlayerId == playerId && x.EventId == gameEvent.Id && x.EventPeriodId == currentPeriod.Id, ct)
        ?? new HrkPlayerEventPeriodProgress
        {
            PlayerId = playerId,
            EventId = gameEvent.Id,
            EventPeriodId = currentPeriod.Id,
            PeriodKey = currentPeriod.PeriodKey,
            CurrentFloor = 1,
            CurrentRunNumber = 1,
            RemainingLives = gameEvent.InitialLives
        };

    public async Task<HrkEventPeriod> GetOrCreateCurrentPeriodAsync(HrkGameEvent gameEvent, CancellationToken ct = default)
    {
        var (startUtc, endUtc, periodKey) = await GetCurrentBoundariesAsync(gameEvent, ct);
        var period = await _unitOfWork.Repository<HrkEventPeriod>().Query()
            .FirstOrDefaultAsync(p => p.EventId == gameEvent.Id && p.PeriodKey == periodKey, ct);
        if (period != null)
        {
            return period;
        }

        var chestSnapshot = await _unitOfWork.ReadOnlyRepository<HrkTowerChestConfig>().Query()
            .Where(x => x.EventId == gameEvent.Id && x.IsActive)
            .Select(x => new EventChestSnapshot(x.Id, x.FloorNumber, x.RewardsJson, x.ChestName, x.ChestIcon, x.Description)).ToListAsync(ct);
        period = new HrkEventPeriod
        {
            EventId = gameEvent.Id,
            PeriodKey = periodKey,
            StartAtUtc = startUtc,
            EndAtUtc = endUtc,
            ChestSnapshotJson = JsonSerializer.Serialize(chestSnapshot),
            CreatedOn = DateTime.UtcNow
        };
        try
        {
            await _unitOfWork.Repository<HrkEventPeriod>().AddAsync(period);
            await _unitOfWork.SaveChangesAsync(ct);
            return period;
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
        {
            _unitOfWork.Detach(period);
            // Concurrent insert race condition: reload created period
            var existing = await _unitOfWork.Repository<HrkEventPeriod>().Query()
                .FirstOrDefaultAsync(p => p.EventId == gameEvent.Id && p.PeriodKey == periodKey, ct);
            if (existing != null)
                return existing;
            throw;
        }
    }

    public async Task<HrkPlayerEventPeriodProgress> GetOrCreatePlayerPeriodProgressAsync(long playerId, HrkGameEvent gameEvent, HrkEventPeriod currentPeriod, CancellationToken ct = default)
    {
        // 1. First ensure unclaimed milestone chests from past periods become pending rewards
        await CloseExpiredPeriodsAsync(playerId, gameEvent, currentPeriod.StartAtUtc, null, ct);
        // 2. Fetch or create progress for current period
        var progress = await _unitOfWork.Repository<HrkPlayerEventPeriodProgress>().Query()
            .FirstOrDefaultAsync(p => p.PlayerId == playerId && p.EventId == gameEvent.Id && p.EventPeriodId == currentPeriod.Id, ct);
        if (progress == null)
        {
            progress = new HrkPlayerEventPeriodProgress
            {
                PlayerId = playerId,
                EventId = gameEvent.Id,
                EventPeriodId = currentPeriod.Id,
                PeriodKey = currentPeriod.PeriodKey,
                CurrentFloor = 1,
                RemainingLives = gameEvent.InitialLives,
                CurrentRunNumber = 1,
                QuickClimbRunsUsed = 0,
                HighestFloorInPeriod = 0,
                IsCompleted = false,
                CreatedOn = DateTime.UtcNow,
                UpdatedOn = DateTime.UtcNow
            };
            try
            {
                await _unitOfWork.Repository<HrkPlayerEventPeriodProgress>().AddAsync(progress);
                await _unitOfWork.SaveChangesAsync(ct);
            }
            catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
            {
                _unitOfWork.Detach(progress);
                // Concurrency race: reload existing
                var existing = await _unitOfWork.Repository<HrkPlayerEventPeriodProgress>().Query()
                    .FirstOrDefaultAsync(p => p.PlayerId == playerId && p.EventId == gameEvent.Id && p.EventPeriodId == currentPeriod.Id, ct);
                if (existing != null)
                {
                    progress = existing;
                }
                else
                {
                    throw;
                }
            }
        }

        // 3. Ensure all-time record exists
        var allTime = await _unitOfWork.Repository<HrkPlayerEventAllTimeRecord>().Query()
            .FirstOrDefaultAsync(a => a.PlayerId == playerId && a.EventId == gameEvent.Id, ct);
        if (allTime == null)
        {
            allTime = new HrkPlayerEventAllTimeRecord
            {
                PlayerId = playerId,
                EventId = gameEvent.Id,
                HighestFloorAllTime = 0,
                TotalClears = 0,
                UpdatedOn = DateTime.UtcNow
            };
            try
            {
                await _unitOfWork.Repository<HrkPlayerEventAllTimeRecord>().AddAsync(allTime);
                await _unitOfWork.SaveChangesAsync(ct);
            }
            catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
            {
                _unitOfWork.Detach(allTime);
                if (!await _unitOfWork.ReadOnlyRepository<HrkPlayerEventAllTimeRecord>().Query().AnyAsync(a => a.PlayerId == playerId && a.EventId == gameEvent.Id, ct))
                    throw;
            }
        }

        if (!await _unitOfWork.ReadOnlyRepository<HrkPlayerTowerRun>().Query().AnyAsync(r => r.PlayerId == playerId && r.EventPeriodId == currentPeriod.Id && r.RunNumber == progress.CurrentRunNumber, ct))
        {
            await _unitOfWork.Repository<HrkPlayerTowerRun>().AddAsync(new HrkPlayerTowerRun { PlayerId = playerId, EventPeriodId = currentPeriod.Id, RunNumber = progress.CurrentRunNumber, StartFloor = progress.CurrentFloor, EndFloor = progress.CurrentFloor, LivesRemaining = progress.RemainingLives, Status = progress.IsCompleted ? "COMPLETED" : "IN_PROGRESS" });
            await _unitOfWork.SaveChangesAsync(ct);
        }

        return progress;
    }

    public async Task<int> CloseExpiredPeriodsAsync(long playerId, HrkGameEvent gameEvent,
        DateTime cutoffUtc, Guid? executionId, CancellationToken ct)
    {
        if (!_unitOfWork.HasActiveTransaction)
            throw new InvalidOperationException("Event settlement requires the caller's player-lock transaction.");
        var progresses = await _unitOfWork.Repository<HrkPlayerEventPeriodProgress>().Query()
            .Include(x => x.EventPeriod)
            .Where(x => x.PlayerId == playerId && x.EventId == gameEvent.Id && x.ClosedAtUtc == null && x.EventPeriod.EndAtUtc <= cutoffUtc)
            .OrderBy(x => x.EventPeriod.EndAtUtc).ToListAsync(ct);
        if (progresses.Count == 0) return 0;
        var player = await _unitOfWork.ReadOnlyRepository<HrkPlayer>().Query().SingleAsync(x => x.Id == playerId, ct);
        var currentChests = await _unitOfWork.ReadOnlyRepository<HrkTowerChestConfig>().Query()
            .Where(x => x.EventId == gameEvent.Id && x.IsActive).OrderBy(x => x.FloorNumber)
            .Select(x => new EventChestSnapshot(x.Id, x.FloorNumber, x.RewardsJson, x.ChestName, x.ChestIcon, x.Description)).ToListAsync(ct);
        foreach (var progress in progresses)
        {
            var period = progress.EventPeriod;
            // Legacy periods have no snapshot; new periods freeze the reward configuration.
            var chests = period.ChestSnapshotJson is null ? currentChests :
                JsonSerializer.Deserialize<List<EventChestSnapshot>>(period.ChestSnapshotJson)
                    ?? throw new InvalidOperationException("Invalid period chest snapshot.");
            var claimed = (await _unitOfWork.ReadOnlyRepository<HrkPlayerEventRewardClaim>().Query()
                .Where(x => x.PlayerId == playerId && x.EventPeriodId == period.Id && x.ClaimType == "MILESTONE_CHEST")
                .Select(x => x.TargetId).ToListAsync(ct)).ToHashSet();
            var pending = (await _unitOfWork.ReadOnlyRepository<HrkPlayerPendingReward>().Query()
                .Where(x => x.PlayerId == playerId && x.EventPeriodId == period.Id && x.SourceType == "UNCLAIMED_MILESTONE_CHEST")
                .Select(x => x.SourceRefId).ToListAsync(ct)).ToHashSet();
            int converted = 0;
            foreach (var chest in chests.Where(x => x.FloorNumber <= progress.HighestFloorInPeriod && !claimed.Contains(x.Id) && !pending.Contains(x.Id)))
            {
                await _unitOfWork.Repository<HrkPlayerPendingReward>().AddAsync(new()
                {
                    PlayerId = playerId, EventPeriodId = period.Id, SourceType = "UNCLAIMED_MILESTONE_CHEST",
                    SourceRefId = chest.Id, Description = $"Milestone chest {chest.FloorNumber} - {period.PeriodKey}",
                    RewardItemsJson = chest.RewardsJson ?? "[]", Status = "PENDING", CreatedOnUtc = DateTime.UtcNow
                });
                converted++;
            }
            var runs = await _unitOfWork.Repository<HrkPlayerTowerRun>().Query()
                .Where(x => x.PlayerId == playerId && x.EventPeriodId == period.Id && x.Status == "IN_PROGRESS").ToListAsync(ct);
            foreach (var run in runs) { run.Status = "EXPIRED"; run.EndAtUtc = period.EndAtUtc; }
            var quickClimbs = await _unitOfWork.Repository<HrkTowerQuickClimbJob>().Query()
                .Where(x => x.PlayerId == playerId && x.EventPeriodId == period.Id && (x.Status == "QUEUED" || x.Status == "PROCESSING")).ToListAsync(ct);
            foreach (var job in quickClimbs)
            {
                job.Status = "EXPIRED";
                job.StopReason = "PERIOD_EXPIRED";
                job.CompletedOnUtc = DateTime.UtcNow;
                job.UpdatedOnUtc = DateTime.UtcNow;
                job.Version++;
                var percentage = Math.Clamp((int)Math.Round(job.ClearedFloorsCount * 100d / Math.Max(1, job.TargetFloor - job.StartFloor + 1)), 0, 100);
                var status = new ProcessStatusUpdatedV1(Guid.NewGuid(), job.JobId, player.UserId, "TOWER_QUICK_CLIMB", job.Version,
                    job.CurrentFloor, job.TargetFloor, percentage, job.Status, DateTime.UtcNow);
                _outbox.Add(status, MessagingContractNames.ProcessStatusUpdatedV1, MessagingPublisherNames.GameEvents,
                    MessagingContractNames.ProcessStatusUpdatedV1, partitionKey: job.JobId, sequence: job.Version);
                _activities.Raise(new QuickClimbEndedEvent(playerId, player.UserId, job.JobId, job.Status, job.StartFloor,
                    job.CurrentFloor, job.TargetFloor, job.ClearedFloorsCount, job.StopReason, job.Version)
                {
                    GameEventId = gameEvent.Id, EventPeriodId = period.Id, PeriodKey = period.PeriodKey, JobExecutionId = executionId
                });
            }
            var summary = new
            {
                progress.HighestFloorInPeriod, progress.CurrentFloor, progress.RemainingLives, progress.CurrentRunNumber,
                progress.QuickClimbRunsUsed, progress.IsCompleted, ConvertedChests = converted,
                ExpiredRuns = runs.Count, ExpiredQuickClimbs = quickClimbs.Count, EffectiveEndAtUtc = period.EndAtUtc
            };
            progress.ClosedAtUtc = DateTime.UtcNow;
            progress.ResetExecutionId = executionId;
            progress.ResetSummaryJson = JsonSerializer.Serialize(summary);
            _activities.Raise(new GameEventActivityEvent(playerId, player.UserId, "PlayerEventPeriodClosed", $"period:{period.Id}", summary)
            {
                GameEventId = gameEvent.Id, EventPeriodId = period.Id, PeriodKey = period.PeriodKey,
                JobExecutionId = executionId, OccurredAt = new DateTimeOffset(DateTime.SpecifyKind(progress.ClosedAtUtc.Value, DateTimeKind.Utc))
            });
        }
        await _unitOfWork.SaveChangesAsync(ct);
        return progresses.Count;
    }

    private async Task<(DateTime startUtc, DateTime endUtc, string periodKey)> GetCurrentBoundariesAsync(
        HrkGameEvent gameEvent, CancellationToken ct)
    {
        if (gameEvent.ResetType != "DAILY")
            throw new InvalidOperationException("Tower events currently support DAILY reset only.");
        var (timeZoneId, resetTime) = await _schedule.GetAsync(gameEvent, ct);
        return EventPeriodCalculator.CalculatePeriodBoundaries(DateTime.UtcNow, timeZoneId, resetTime);
    }
}
