using Core.Common.Errors;
using Core.Persistence.Repositories;
using GAME.Application.Common;
using GAME.Application.Interfaces;
using GAME.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GAME.Application.Services;

/// <summary>Persists the default formation's record in the caller's business transaction.</summary>
public sealed class PlayerRankingWriter(IUnitOfWork uow, IFormationPowerQueryService power,
    IGameTransactionLock locks, IPlayerRankingStatsStore statsStore, TimeProvider clock) : IPlayerRankingWriter
{
    public async Task RefreshPowerAsync(long playerId, CancellationToken ct = default)
    {
        await locks.AcquireAsync(GameLockResources.PlayerState(playerId), ct);
        // Queries must see changes to heroes, equipment and IsSelected in this transaction.
        await uow.SaveChangesAsync(ct);
        var value = (await power.GetDefaultFormationPowerAsync(playerId, ct)).TotalPower;
        var stats = await statsStore.GetOrCreateAsync(playerId, ct);
        stats.RecordFormationPower(value, clock.GetUtcNow().UtcDateTime);
        await uow.SaveChangesAsync(ct);
    }

    public async Task<T> ExecutePowerChangeAsync<T>(string userId, Func<Task<T>> action, CancellationToken ct = default)
    {
        T result = default!;
        await uow.ExecuteInTransactionAsync(async () =>
        {
            // The UOW rejects pending changes before entering; rebuild reads inside this operation.
            uow.ClearTracking();
            var playerId = await uow.ReadOnlyRepository<HrkPlayer>().Query()
                .Where(x => x.UserId == userId && x.IsActive).Select(x => (long?)x.Id).SingleOrDefaultAsync(ct)
                ?? throw new BusinessException("PLAYER_NOT_FOUND", "Không tìm thấy người chơi.", BusinessErrorKind.NotFound);
            await locks.AcquireAsync(GameLockResources.PlayerState(playerId), ct);
            result = await action();
            await RefreshPowerAsync(playerId, ct);
        }, ct);
        return result;
    }
}
