using GAME.Application.DTOs;

namespace GAME.Application.Interfaces;

public interface IDungeonChestReader
{
    Task<List<DungeonStarChestDto>> GetMapChestsAsync(long playerId, int mapId, int totalStars, CancellationToken cancellationToken = default);
}
