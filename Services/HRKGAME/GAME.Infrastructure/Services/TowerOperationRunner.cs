using Core.Common.Repositories;
using GAME.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GAME.Infrastructure.Services;

/// <summary>
/// Serializes tower operations per player across API instances. Each operation/floor
/// owns one transaction; no long-lived lease or in-memory lock is required.
/// </summary>
public sealed class TowerOperationRunner(IUnitOfWork unitOfWork, GameDbContext db)
{
    public async Task<T> RunAsync<T>(string userId, Func<Task<T>> action, CancellationToken ct)
    {
        if (db.Database.CurrentTransaction != null)
            return await action();

        T result = default!;
        await unitOfWork.ExecuteStrategyAsync(async () =>
        {
            db.ChangeTracker.Clear(); // Rebuild state on every execution-strategy retry.
            await unitOfWork.BeginTransactionAsync();
            try
            {
                await LockAsync("tower:player:" + userId, ct);
                result = await action();
                await unitOfWork.SaveChangesAsync(ct);
                await unitOfWork.CommitTransactionAsync();
            }
            catch
            {
                await unitOfWork.RollbackTransactionAsync();
                db.ChangeTracker.Clear();
                throw;
            }
        });
        return result;
    }

    public Task LockAsync(string resource, CancellationToken ct) =>
        db.Database.ExecuteSqlInterpolatedAsync($@"
            DECLARE @result int;
            EXEC @result = sys.sp_getapplock
                @Resource = {resource}, @LockMode = 'Exclusive',
                @LockOwner = 'Transaction', @LockTimeout = 15000;
            IF @result < 0 THROW 51000, 'Tower operation is busy. Please retry.', 1;", ct);
}
