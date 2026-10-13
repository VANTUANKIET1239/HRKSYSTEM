using GAME.Application.DTOs;

namespace GAME.Application.Interfaces;

public interface IDungeonStarService
{
    Task<(int earnedStars, decimal remainingHpRate)> CalculateEarnedStarsAsync(int mapId, StartBattleResultDto battle, CancellationToken cancellationToken = default);
    Task<int> GetMapTotalStarsAsync(long playerId, int mapId, CancellationToken cancellationToken = default);
    Task<Dictionary<int, int>> GetStageBestStarsAsync(long playerId, IEnumerable<int> stageIds, CancellationToken cancellationToken = default);
}
