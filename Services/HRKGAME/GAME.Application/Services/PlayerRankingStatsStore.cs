using Core.Persistence.Repositories;
using GAME.Application.Common;
using GAME.Application.Interfaces;
using GAME.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GAME.Application.Services;

public sealed class PlayerRankingStatsStore(IUnitOfWork uow, IGameTransactionLock locks, TimeProvider clock) : IPlayerRankingStatsStore
{
    public async Task<HrkPlayerRankingStats> GetOrCreateAsync(long playerId, CancellationToken ct = default)
    {
        await locks.AcquireAsync(GameLockResources.PlayerState(playerId), ct);
        var repository = uow.Repository<HrkPlayerRankingStats>();
        var stats = await repository.Query().SingleOrDefaultAsync(x => x.PlayerId == playerId, ct);
        if (stats is not null) return stats;
        var now = clock.GetUtcNow().UtcDateTime;
        stats = new()
        {
            PlayerId = playerId, PeakPowerAchievedAtUtc = now, DiamondsSpentAchievedAtUtc = now,
            TrackingStartedAtUtc = now, UpdatedAtUtc = now
        };
        await repository.AddAsync(stats);
        // Persist within the caller's transaction so another lookup sees the same tracked row.
        await uow.SaveChangesAsync(ct);
        return stats;
    }
}
