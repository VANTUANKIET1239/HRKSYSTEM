using GAME.Application.DTOs;

namespace GAME.Application.Interfaces;

public interface IDungeonService
{
    Task<List<DungeonMapDto>> GetMapsAsync(string userId, CancellationToken cancellationToken = default);
    Task<DungeonMapDetailDto> GetMapAsync(string userId, int mapId, CancellationToken cancellationToken = default);
    Task<StartDungeonStageResultDto> StartStageAsync(string userId, int stageId, StartDungeonStageRequestDto request, CancellationToken cancellationToken = default);
    Task<DungeonStaminaDto> GetStaminaAsync(string userId, CancellationToken cancellationToken = default);
    Task<DungeonStaminaDto> PurchaseStaminaAsync(string userId, CancellationToken cancellationToken = default);
    Task<ClaimStarChestResultDto> ClaimStarChestAsync(string userId, int chestId, CancellationToken cancellationToken = default);
}
