using GAME.Domain.Entities;

namespace GAME.Application.Interfaces;

public interface IDiamondSpendingService
{
    Task<bool> IsReplayAsync(long playerId, Guid requestId, string reason, string reference, CancellationToken ct = default);
    Task<bool> SpendAsync(HrkPlayerWallet wallet, long amount, string reason, string reference, Guid requestId, CancellationToken ct = default);
}
