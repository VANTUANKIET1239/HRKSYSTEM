using GAME.Application.DTOs;

namespace GAME.Application.Interfaces;

public interface IDungeonChestService
{
    Task<List<DungeonStarChestDto>> GetMapChestsAsync(long playerId, int mapId, int totalStars, CancellationToken cancellationToken = default);
    Task<ClaimStarChestResultDto> ClaimStarChestAsync(string userId, int chestId, CancellationToken cancellationToken = default);
}
