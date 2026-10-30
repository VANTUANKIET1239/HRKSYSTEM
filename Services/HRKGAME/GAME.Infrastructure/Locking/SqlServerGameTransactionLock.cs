using Core.Persistence.Repositories;
using GAME.Application.Common;
using GAME.Application.Interfaces;

namespace GAME.Infrastructure.Locking;

/// <summary>Uses stored procedures on the UnitOfWork's existing connection and transaction.</summary>
public sealed class SqlServerGameTransactionLock(IUnitOfWork uow) : IGameTransactionLock
{
    private Guid? _transactionId;
    private readonly HashSet<string> _resources = new(StringComparer.Ordinal);
    private readonly HashSet<long> _wallets = [];

    public async Task AcquireAsync(string resource, CancellationToken ct = default, int timeoutMilliseconds = 15_000)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resource);
        if (resource.Length > 255) throw new ArgumentException("Lock resources cannot exceed 255 characters.", nameof(resource));
        ArgumentOutOfRangeException.ThrowIfNegative(timeoutMilliseconds);
        ct.ThrowIfCancellationRequested();
        CheckTransaction();
        if (_resources.Contains(resource)) return;
        await uow.Sql.ExecuteProcedureAsync(GameStoredProcedures.AcquireTransactionLock,
            new { Resource = resource, LockTimeoutMilliseconds = timeoutMilliseconds }, ct);
        // Remember successful acquisitions only; a failed wait can be attempted again.
        _resources.Add(resource);
    }

    public async Task LockWalletAsync(long playerId, CancellationToken ct = default)
    {
        await AcquireAsync(GameLockResources.PlayerState(playerId), ct);
        if (_wallets.Contains(playerId)) return;
        await uow.Sql.ExecuteProcedureAsync(GameStoredProcedures.LockPlayerWallet, new { PlayerId = playerId }, ct);
        _wallets.Add(playerId);
    }

    private void CheckTransaction()
    {
        var transactionId = uow.CurrentTransactionId;
        if (!uow.HasActiveTransaction || transactionId is null)
            throw new InvalidOperationException("Database locks require an active UnitOfWork transaction.");
        if (_transactionId == transactionId) return;
        // Transaction IDs change on retries and after commit/rollback. Never reuse a previous lock claim.
        _transactionId = transactionId;
        _resources.Clear();
        _wallets.Clear();
    }
}
