using Core.Common.Repositories;
using GAME.Infrastructure.Data;
using Microsoft.Data.SqlClient;
using GAME.Application.Interfaces;
using GAME.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace GAME.Infrastructure.Services;

public class EventPeriodService : IEventPeriodService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly GameDbContext? _db;

    public EventPeriodService(IUnitOfWork unitOfWork, GameDbContext? db = null)
    {
        _unitOfWork = unitOfWork;
        _db = db;
    }

    public TimeZoneInfo GetEventTimeZone(string timeZoneId)
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
        catch
        {
            if (timeZoneId.Contains("Ho_Chi_Minh", StringComparison.OrdinalIgnoreCase) ||
                timeZoneId.Contains("Bangkok", StringComparison.OrdinalIgnoreCase) ||
                timeZoneId.Contains("Hanoi", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    return TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
                }
                catch
                {
                    return TimeZoneInfo.CreateCustomTimeZone("Asia/Ho_Chi_Minh", TimeSpan.FromHours(7), "Indochina Time", "Indochina Time");
                }
            }

            throw new InvalidOperationException($"Múi giờ sự kiện không hợp lệ: {timeZoneId}");
        }
    }

    public (DateTime startUtc, DateTime endUtc, string periodKey) CalculatePeriodBoundaries(DateTime utcNow, string timeZoneId, TimeSpan resetTime)
    {
        var tz = GetEventTimeZone(timeZoneId);
        var localTime = TimeZoneInfo.ConvertTimeFromUtc(utcNow, tz);

        DateTime localStart;
        DateTime localEnd;

        if (localTime.TimeOfDay < resetTime)
        {
            localStart = localTime.Date.AddDays(-1).Add(resetTime);
            localEnd = localTime.Date.Add(resetTime);
        }
        else
        {
            localStart = localTime.Date.Add(resetTime);
            localEnd = localTime.Date.AddDays(1).Add(resetTime);
        }

        var startUtc = TimeZoneInfo.ConvertTimeToUtc(localStart, tz);
        var endUtc = TimeZoneInfo.ConvertTimeToUtc(localEnd, tz);
        var periodKey = localStart.ToString("yyyy-MM-dd");

        return (startUtc, endUtc, periodKey);
    }

    public async Task<HrkEventPeriod> GetOrCreateCurrentPeriodAsync(HrkGameEvent gameEvent, CancellationToken ct = default)
    {
        if (gameEvent.ResetType != "DAILY")
            throw new InvalidOperationException("Hiện tại chỉ hỗ trợ ResetType DAILY; không được âm thầm bỏ qua cấu hình khác.");
        if (gameEvent.ResetTime < TimeSpan.Zero || gameEvent.ResetTime >= TimeSpan.FromDays(1))
            throw new InvalidOperationException("Giờ reset phải nằm trong một ngày.");
        var (startUtc, endUtc, periodKey) = CalculatePeriodBoundaries(DateTime.UtcNow, gameEvent.TimeZoneId, gameEvent.ResetTime);

        var period = await _unitOfWork.Repository<HrkEventPeriod>().Query()
            .FirstOrDefaultAsync(p => p.EventId == gameEvent.Id && p.PeriodKey == periodKey, ct);

        if (period != null)
        {
            return period;
        }

        period = new HrkEventPeriod
        {
            EventId = gameEvent.Id,
            PeriodKey = periodKey,
            StartAtUtc = startUtc,
            EndAtUtc = endUtc,
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
            _db!.Entry(period).State = EntityState.Detached;
            // Concurrent insert race condition: reload created period
            var existing = await _unitOfWork.Repository<HrkEventPeriod>().Query()
                .FirstOrDefaultAsync(p => p.EventId == gameEvent.Id && p.PeriodKey == periodKey, ct);
            if (existing != null) return existing;
            throw;
        }
    }

    public async Task CheckAndConvertUnclaimedChestsToPendingRewardsAsync(
        long playerId, HrkGameEvent gameEvent, HrkEventPeriod currentPeriod, CancellationToken ct = default)
    {
        // Find previous period progresses for this player and event that ended before current period
        var pastProgresses = await _unitOfWork.Repository<HrkPlayerEventPeriodProgress>().Query()
            .Include(p => p.EventPeriod)
            .Where(p => p.PlayerId == playerId && p.EventId == gameEvent.Id && p.EventPeriodId != currentPeriod.Id)
            .ToListAsync(ct);

        if (pastProgresses.Count == 0) return;

        var allChests = await _unitOfWork.ReadOnlyRepository<HrkTowerChestConfig>().Query()
            .Where(c => c.EventId == gameEvent.Id && c.IsActive)
            .OrderBy(c => c.FloorNumber)
            .ToListAsync(ct);

        if (allChests.Count == 0) return;

        var pastPeriodIds = pastProgresses.Select(p => p.EventPeriodId).ToList();

        var existingClaims = await _unitOfWork.ReadOnlyRepository<HrkPlayerEventRewardClaim>().Query()
            .Where(c => c.PlayerId == playerId && pastPeriodIds.Contains(c.EventPeriodId) && c.ClaimType == "MILESTONE_CHEST")
            .Select(c => new { c.EventPeriodId, c.TargetId })
            .ToListAsync(ct);

        var existingPendingRewards = await _unitOfWork.ReadOnlyRepository<HrkPlayerPendingReward>().Query()
            .Where(r => r.PlayerId == playerId && pastPeriodIds.Contains(r.EventPeriodId) && r.SourceType == "UNCLAIMED_MILESTONE_CHEST")
            .Select(r => new { r.EventPeriodId, r.SourceRefId })
            .ToListAsync(ct);

        var claimsSet = new HashSet<(long PeriodId, int ChestId)>(existingClaims.Select(c => (c.EventPeriodId, c.TargetId)));
        var pendingSet = new HashSet<(long PeriodId, int ChestId)>(existingPendingRewards.Select(r => (r.EventPeriodId, r.SourceRefId)));

        bool hasNewPending = false;
        foreach (var pastProgress in pastProgresses)
        {
            var eligibleChests = allChests.Where(c => c.FloorNumber <= pastProgress.HighestFloorInPeriod);
            foreach (var chest in eligibleChests)
            {
                if (claimsSet.Contains((pastProgress.EventPeriodId, chest.Id))) continue;
                if (pendingSet.Contains((pastProgress.EventPeriodId, chest.Id))) continue;

                var pendingReward = new HrkPlayerPendingReward
                {
                    PlayerId = playerId,
                    EventPeriodId = pastProgress.EventPeriodId,
                    SourceType = "UNCLAIMED_MILESTONE_CHEST",
                    SourceRefId = chest.Id,
                    Description = $"Rương mốc Tầng {chest.FloorNumber} ({chest.ChestName}) - Kỳ {pastProgress.PeriodKey}",
                    RewardItemsJson = chest.RewardsJson ?? "[]",
                    Status = "PENDING",
                    CreatedOnUtc = DateTime.UtcNow
                };

                await _unitOfWork.Repository<HrkPlayerPendingReward>().AddAsync(pendingReward);
                pendingSet.Add((pastProgress.EventPeriodId, chest.Id));
                hasNewPending = true;
            }
        }

        if (hasNewPending)
        {
            await _unitOfWork.SaveChangesAsync(ct);
        }
    }

    public async Task<HrkPlayerEventPeriodProgress> GetOrCreatePlayerPeriodProgressAsync(
        long playerId, HrkGameEvent gameEvent, HrkEventPeriod currentPeriod, CancellationToken ct = default)
    {
        // 1. First ensure unclaimed milestone chests from past periods become pending rewards
        await CheckAndConvertUnclaimedChestsToPendingRewardsAsync(playerId, gameEvent, currentPeriod, ct);

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
                _db!.Entry(progress).State = EntityState.Detached;
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
                _db!.Entry(allTime).State = EntityState.Detached;
                if (!await _unitOfWork.ReadOnlyRepository<HrkPlayerEventAllTimeRecord>().Query()
                    .AnyAsync(a => a.PlayerId == playerId && a.EventId == gameEvent.Id, ct)) throw;
            }
        }

        if (!await _unitOfWork.ReadOnlyRepository<HrkPlayerTowerRun>().Query()
            .AnyAsync(r => r.PlayerId == playerId && r.EventPeriodId == currentPeriod.Id &&
                r.RunNumber == progress.CurrentRunNumber, ct))
        {
            await _unitOfWork.Repository<HrkPlayerTowerRun>().AddAsync(new HrkPlayerTowerRun
            {
                PlayerId = playerId,
                EventPeriodId = currentPeriod.Id,
                RunNumber = progress.CurrentRunNumber,
                StartFloor = progress.CurrentFloor,
                EndFloor = progress.CurrentFloor,
                LivesRemaining = progress.RemainingLives,
                Status = progress.IsCompleted ? "COMPLETED" : "IN_PROGRESS"
            });
            await _unitOfWork.SaveChangesAsync(ct);
        }
        var expiredRuns = await _unitOfWork.Repository<HrkPlayerTowerRun>().Query()
            .Where(r => r.PlayerId == playerId && r.EventPeriod.EventId == gameEvent.Id &&
                r.EventPeriod.EndAtUtc <= currentPeriod.StartAtUtc && r.Status == "IN_PROGRESS")
            .ToListAsync(ct);
        foreach (var run in expiredRuns)
        {
            run.Status = "EXPIRED";
            run.EndAtUtc = currentPeriod.StartAtUtc;
        }
        return progress;
    }

    public long CalculateRemainingSecondsToReset(HrkEventPeriod currentPeriod, DateTime? utcNow = null)
    {
        var now = utcNow ?? DateTime.UtcNow;
        var diff = (currentPeriod.EndAtUtc - now).TotalSeconds;
        return (long)Math.Max(0, diff);
    }
}
