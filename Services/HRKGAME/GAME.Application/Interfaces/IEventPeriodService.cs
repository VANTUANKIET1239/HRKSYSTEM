using GAME.Application.DTOs;
using GAME.Domain.Entities;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace GAME.Application.Interfaces;

public interface IEventPeriodService
{
    TimeZoneInfo GetEventTimeZone(string timeZoneId);
    (DateTime startUtc, DateTime endUtc, string periodKey) CalculatePeriodBoundaries(DateTime utcNow, string timeZoneId, TimeSpan resetTime);
    Task<HrkEventPeriod> GetOrCreateCurrentPeriodAsync(HrkGameEvent gameEvent, CancellationToken ct = default);
    Task<HrkPlayerEventPeriodProgress> GetOrCreatePlayerPeriodProgressAsync(long playerId, HrkGameEvent gameEvent, HrkEventPeriod currentPeriod, CancellationToken ct = default);
    Task CheckAndConvertUnclaimedChestsToPendingRewardsAsync(long playerId, HrkGameEvent gameEvent, HrkEventPeriod currentPeriod, CancellationToken ct = default);
    long CalculateRemainingSecondsToReset(HrkEventPeriod currentPeriod, DateTime? utcNow = null);
}
