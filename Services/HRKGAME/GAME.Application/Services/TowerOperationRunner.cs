using Core.Persistence.Repositories;
using GAME.Application.Common;

namespace GAME.Application.Services;
/// <summary>
/// Serializes tower operations per player across API instances. Each operation/floor
/// owns one transaction; no long-lived lease or in-memory lock is required.
/// </summary>
public sealed class TowerOperationRunner(IUnitOfWork unitOfWork)
{
    public async Task<T> RunAsync<T>(string userId, Func<Task<T>> action, CancellationToken ct)
    {
        if (unitOfWork.HasActiveTransaction)
        {
            await LockAsync("tower:player:" + userId, ct);
            return await action();
        }

        T result = default!;
        await unitOfWork.ExecuteStrategyAsync(async () =>
        {
            unitOfWork.ClearTracking(); // Rebuild state on every execution-strategy retry.
            await unitOfWork.BeginTransactionAsync(ct);
            try
            {
                await LockAsync("tower:player:" + userId, ct);
                result = await action();
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
        unitOfWork.Sql.ExecuteProcedureAsync(GameStoredProcedures.AcquireTowerPlayerLock,
            new { Resource = resource }, ct);
}
