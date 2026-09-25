using GAME.Application.DTOs;

namespace GAME.Application.Interfaces;

public interface IDungeonStarService
{
    Task<(int earnedStars, decimal remainingHpRate)> CalculateEarnedStarsAsync(
        int mapId, StartBattleResultDto battle, CancellationToken cancellationToken = default);
    Task<(int earnedStars, int previousBestStars, int bestStars, bool isNewRecord, decimal remainingHpRate)> RecordStageProgressAsync(
        long playerId, int stageId, StartBattleResultDto battle, CancellationToken cancellationToken = default);
    Task<int> GetMapTotalStarsAsync(long playerId, int mapId, CancellationToken cancellationToken = default);
    Task<Dictionary<int, int>> GetStageBestStarsAsync(long playerId, IEnumerable<int> stageIds, CancellationToken cancellationToken = default);
}
