using GAME.Domain.Entities;

namespace GAME.Application.Interfaces;

public interface ITowerEventService
{
    Task<HrkEventPeriod> ReadCurrentPeriodAsync(HrkGameEvent gameEvent, CancellationToken ct = default);
    Task<HrkPlayerEventPeriodProgress> ReadPlayerPeriodProgressAsync(long playerId, HrkGameEvent gameEvent,
        HrkEventPeriod currentPeriod, CancellationToken ct = default);
    Task<HrkEventPeriod> GetOrCreateCurrentPeriodAsync(HrkGameEvent gameEvent, CancellationToken ct = default);
    Task<HrkPlayerEventPeriodProgress> GetOrCreatePlayerPeriodProgressAsync(long playerId, HrkGameEvent gameEvent,
        HrkEventPeriod currentPeriod, CancellationToken ct = default);

    // Caller owns the player's TowerOperationRunner transaction and SQL lock.
    Task<int> CloseExpiredPeriodsAsync(long playerId, HrkGameEvent gameEvent,
        DateTime cutoffUtc, Guid? executionId, CancellationToken ct);
}
