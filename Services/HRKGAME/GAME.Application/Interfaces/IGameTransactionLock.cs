namespace GAME.Application.Interfaces;

/// <summary>Locks belong to the current UnitOfWork transaction and are released on commit/rollback.</summary>
public interface IGameTransactionLock
{
    Task AcquireAsync(string resource, CancellationToken ct = default, int timeoutMilliseconds = 15_000);
    /// <summary>Acquires player state first, then locks the wallet row against concurrent SQL updates.</summary>
    Task LockWalletAsync(long playerId, CancellationToken ct = default);
}
