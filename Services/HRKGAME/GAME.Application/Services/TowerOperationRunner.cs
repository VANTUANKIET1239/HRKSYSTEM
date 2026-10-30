using Core.Persistence.Repositories;
using GAME.Application.Common;
using GAME.Application.Interfaces;
using GAME.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GAME.Application.Services;
/// <summary>
/// Serializes tower operations per player across API instances. Each operation/floor
/// owns one transaction; no long-lived lease or in-memory lock is required.
/// </summary>
public sealed class TowerOperationRunner(IUnitOfWork unitOfWork, IPlayerRankingWriter rankings, IGameTransactionLock locks)
{
    public async Task<T> RunAsync<T>(string userId, Func<Task<T>> action, CancellationToken ct)
    {
        if (unitOfWork.HasActiveTransaction)
        {
            await LockAsync(GameLockResources.TowerPlayer(userId), ct);
            return await action();
        }

        T result = default!;
        await unitOfWork.ExecuteStrategyAsync(async () =>
        {
            unitOfWork.ClearTracking(); // Rebuild state on every execution-strategy retry.
            await unitOfWork.BeginTransactionAsync(ct);
            try
            {
                await LockAsync(GameLockResources.TowerPlayer(userId), ct);
                var playerId = await unitOfWork.ReadOnlyRepository<HrkPlayer>().Query().Where(x => x.UserId == userId && x.IsActive).Select(x => (long?)x.Id).SingleOrDefaultAsync(ct);
                if (playerId.HasValue) await locks.AcquireAsync(GameLockResources.PlayerState(playerId.Value), ct);
                result = await action();
                if (playerId.HasValue) await rankings.RefreshPowerAsync(playerId.Value, ct);
                await unitOfWork.SaveChangesAsync(ct);
                await unitOfWork.CommitTransactionAsync(ct);
            }
            catch
            {
                try
                {
                    await unitOfWork.RollbackTransactionAsync();
                }
                finally
                {
                    unitOfWork.ClearTracking();
                }

                throw;
            }
        }, ct);
        return result;
    }

    public Task LockAsync(string resource, CancellationToken ct) =>
        locks.AcquireAsync(resource, ct);
}
