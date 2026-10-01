using GAME.Application.DTOs;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GAME.Application.Interfaces;

public interface ITowerClimbService
{
    Task<List<GameEventDto>> GetEventsAsync(string userId, CancellationToken ct = default);
    Task<GameEventDto> GetEventDetailAsync(string userId, string eventCode, CancellationToken ct = default);
    Task<PlayerEventProgressDto> GetPlayerProgressAsync(string userId, CancellationToken ct = default);
    Task<List<TowerFloorDto>> GetFloorsAsync(string userId, CancellationToken ct = default);
    Task<TowerFloorDetailDto> GetFloorDetailAsync(string userId, int floorNumber, CancellationToken ct = default);
    Task<StartTowerBattleResultDto> StartFloorBattleAsync(string userId, int floorNumber, StartTowerBattleRequestDto request, CancellationToken ct = default);
    Task<PlayerEventProgressDto> StartNewRunAsync(string userId, CancellationToken ct = default);
    Task<ClaimRewardResultDto> ClaimMilestoneChestAsync(string userId, int chestId, CancellationToken ct = default);
    Task<List<PlayerPendingRewardDto>> GetPendingRewardsAsync(string userId, CancellationToken ct = default);
    Task<ClaimRewardResultDto> ClaimPendingRewardAsync(string userId, long pendingRewardId, CancellationToken ct = default);
    Task<StartTowerBattleResultDto?> GetBattleDetailAsync(string userId, string battleId, CancellationToken ct = default);
}
