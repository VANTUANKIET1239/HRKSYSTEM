using GAME.Domain.Entities;

namespace GAME.Application.Interfaces;

public interface IPlayerRankingStatsStore
{
    /// <summary>Returns a tracked ranking entity, creating it under the player-state lock if needed.</summary>
    Task<HrkPlayerRankingStats> GetOrCreateAsync(long playerId, CancellationToken ct = default);
}
