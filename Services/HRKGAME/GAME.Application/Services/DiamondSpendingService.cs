using Core.Common.Errors;
using Core.Persistence.Repositories;
using GAME.Application.Common;
using GAME.Application.Interfaces;
using GAME.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GAME.Application.Services;

public sealed class DiamondSpendingService(IUnitOfWork uow, IGameTransactionLock locks,
    IPlayerRankingStatsStore statsStore, TimeProvider clock) : IDiamondSpendingService
{
    public async Task<bool> IsReplayAsync(long playerId, Guid requestId, string reason, string reference, CancellationToken ct = default)
    {
        await locks.AcquireAsync(GameLockResources.PlayerState(playerId), ct);
        var previous = await uow.ReadOnlyRepository<HrkPlayerDiamondSpendTransaction>().Query()
            .SingleOrDefaultAsync(x => x.PlayerId == playerId && x.RequestId == requestId, ct);
        if (previous is null) return false;
        if (previous.Reason != reason || previous.Reference != reference)
            throw new BusinessException("DIAMOND_REQUEST_CONFLICT", "Mã giao dịch đã được sử dụng cho yêu cầu khác.", BusinessErrorKind.Conflict);
        return true;
    }

    public async Task<bool> SpendAsync(HrkPlayerWallet wallet, long amount, string reason, string reference, Guid requestId, CancellationToken ct = default)
    {
        if (requestId == Guid.Empty || amount < 0 || amount > int.MaxValue || string.IsNullOrWhiteSpace(reason) || reason.Length > 40 || reference.Length > 120)
            throw new BusinessException("INVALID_DIAMOND_SPEND", "Giao dịch kim cương không hợp lệ.", BusinessErrorKind.Validation);
        if (!uow.IsTracked(wallet)) throw new InvalidOperationException("Diamond spending requires a tracked wallet.");
        await locks.AcquireAsync(GameLockResources.PlayerState(wallet.PlayerId), ct);
        var previous = await uow.ReadOnlyRepository<HrkPlayerDiamondSpendTransaction>().Query()
            .SingleOrDefaultAsync(x => x.PlayerId == wallet.PlayerId && x.RequestId == requestId, ct);
        if (previous is not null)
        {
            if (previous.Amount != amount || previous.Reason != reason || previous.Reference != reference)
                throw new BusinessException("DIAMOND_REQUEST_CONFLICT", "Mã giao dịch đã được sử dụng cho yêu cầu khác.", BusinessErrorKind.Conflict);
            return false;
        }
        if (wallet.Diamonds < amount)
            throw new BusinessException("INSUFFICIENT_DIAMONDS", "Không đủ kim cương.", BusinessErrorKind.Conflict);
        var stats = await statsStore.GetOrCreateAsync(wallet.PlayerId, ct);
        var now = clock.GetUtcNow().UtcDateTime;
        wallet.Diamonds -= checked((int)amount);
        wallet.UpdatedOn = now;
        await uow.Repository<HrkPlayerDiamondSpendTransaction>().AddAsync(new()
        {
            PlayerId = wallet.PlayerId, RequestId = requestId, Amount = amount, Reason = reason,
            Reference = reference, CreatedAtUtc = now
        });
        stats.RecordDiamondSpend(amount, now);
        // Make the receipt visible to subsequent calls in this same transaction, and
        // detect stale wallet RowVersion before the caller grants the purchased benefit.
        await uow.SaveChangesAsync(ct);
        return true;
    }
}
