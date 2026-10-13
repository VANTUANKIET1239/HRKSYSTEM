using GAME.Domain.Entities;

namespace GAME.Application.Interfaces;
/// <summary>Shared entity lookups. Does not run player use cases or save changes.</summary>
public interface IGamePlayerService
{
    Task<HrkPlayer?> GetPlayerByUserIdAsync(string userId, CancellationToken cancellationToken = default, bool tracked = false);
    Task<HrkPlayer?> GetPlayerByIdAsync(long playerId, CancellationToken cancellationToken = default, bool tracked = false);
    Task<HrkPlayerWallet?> GetWalletByPlayerIdAsync(long playerId, CancellationToken cancellationToken = default);
}
