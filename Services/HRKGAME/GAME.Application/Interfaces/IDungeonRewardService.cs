using GAME.Application.DTOs;

namespace GAME.Application.Interfaces;

public interface IDungeonRewardService
{
    Task<List<DungeonPossibleDropDto>> GetPossibleDropsForStageAsync(int stageId, CancellationToken cancellationToken = default);
    Task<Dictionary<int, List<DungeonPossibleDropDto>>> GetPossibleDropsForStagesAsync(
        IReadOnlyCollection<int> stageIds,
        CancellationToken cancellationToken = default);
    Task<(DungeonDroppedEquipmentDto? droppedEquipment, bool isBagFull)> RollEquipmentDropAsync(
        long playerId, int stageId, bool isFirstClear, CancellationToken cancellationToken = default);
    Task<bool> IsBagFullAsync(long playerId, CancellationToken cancellationToken = default);
}
