using Core.Common.Errors;
using Core.Persistence.Repositories;
using GAME.Application.Common;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;
using GAME.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Core.Application.CQRS;
using Core.Common.Entity.MyCompany.Shared.Responses;
using GAME.Application.Services;
using GAME.Application.Features.Shared.Tower;

namespace GAME.Application.Features.Queries.Tower;

public sealed class GetFloorDetailQueryHandler(
    TowerBattleExecutor executor,
    IUnitOfWork unitOfWork,
    IGamePlayerService gamePlayerService,
    IFormationSnapshotService formationSnapshotService,
    IFormationPowerQueryService formationPowerQueryService,
    ITowerEventService towerEvents)
    : TowerHandlerSupport(executor, unitOfWork, gamePlayerService, formationSnapshotService, formationPowerQueryService, towerEvents), IQueryHandler<GetFloorDetailQuery, BaseResponse<TowerFloorDetailDto>>
{
    public async Task<BaseResponse<TowerFloorDetailDto>> Handle(GetFloorDetailQuery message, CancellationToken ct) => BaseResponse<TowerFloorDetailDto>.SuccessResponse(await ExecuteAsync(message.UserId, message.FloorNumber, ct));
    private Task<TowerFloorDetailDto> ExecuteAsync(string userId, int floorNumber, CancellationToken ct = default) => GetFloorDetailAsyncCore(userId, floorNumber, ct);
    private async Task<TowerFloorDetailDto> GetFloorDetailAsyncCore(string userId, int floorNumber, CancellationToken ct = default)
    {
        var player = await _gamePlayerService.GetPlayerByUserIdAsync(userId, ct) ?? throw new BusinessException("PLAYER_NOT_FOUND", "Không tìm thấy người chơi.", BusinessErrorKind.NotFound);
        var ev = await GetTowerEventAsync(ct);
        var period = await _towerEvents.ReadCurrentPeriodAsync(ev, ct);
        var progress = await _towerEvents.ReadPlayerPeriodProgressAsync(player.Id, ev, period, ct);
        var floor = await _unitOfWork.ReadOnlyRepository<HrkTowerFloor>().Query()
            .Where(f => f.EventId == ev.Id && f.FloorNumber == floorNumber && f.IsActive)
            .Include(f => f.Enemies)
            .FirstOrDefaultAsync(ct) ?? throw BusinessException.Localized("FLOOR_NOT_FOUND", $"Không tìm thấy tầng {floorNumber}.", BusinessErrorKind.NotFound);
        var chests = (await GetPeriodChestsAsync(period, ct)).FirstOrDefault(c => c.FloorNumber == floorNumber);
        var claimedChestIds = (await _unitOfWork.ReadOnlyRepository<HrkPlayerEventRewardClaim>().Query()
            .Where(c => c.PlayerId == player.Id && c.EventPeriodId == period.Id && c.ClaimType == "MILESTONE_CHEST")
            .Select(c => c.TargetId)
            .ToListAsync(ct)).ToHashSet();
        string state = progress.IsCompleted || floor.FloorNumber < progress.CurrentFloor ? "CLEARED" : (floor.FloorNumber == progress.CurrentFloor ? "CURRENT" : "LOCKED");
        TowerChestDto? chestDto = null;
        if (chests != null)
        {
            chestDto = new TowerChestDto
            {
                Id = chests.Id,
                FloorNumber = chests.FloorNumber,
                ChestName = chests.ChestName,
                ChestIcon = chests.ChestIcon,
                Description = chests.Description,
                State = claimedChestIds.Contains(chests.Id) ? "CLAIMED" : (progress.HighestFloorInPeriod >= chests.FloorNumber ? "UNLOCKED" : "LOCKED"),
                Rewards = ParseRewardsJson(chests.RewardsJson)
            };
        }

        var playerPower = await GetSelectedFormationPower(player.Id, ct);
        var enemyDtos = (await _executor.BuildEnemiesAsync(floor, ct, includeSkills: false)).Select(e => ToEnemyDto(e, floor.FloorType == "BOSS" && e.Position == 1)).ToList();
        bool canStart = floorNumber <= await _executor.MaxFloorAsync(ev.Id, ct) && state == "CURRENT" && progress.RemainingLives > 0 && !progress.IsCompleted && EventAccessPolicy.IsOpen(ev, DateTime.UtcNow) && player.Level >= ev.MinPlayerLevel;
        string? lockedReason = !EventAccessPolicy.IsOpen(ev, DateTime.UtcNow) ? "Sự kiện chưa mở hoặc đã kết thúc." : player.Level < ev.MinPlayerLevel ? $"Cần cấp {ev.MinPlayerLevel} để tham gia." : null;
        if (progress.RemainingLives <= 0)
        {
            lockedReason = "Đã hết mạng trong lượt leo hiện tại. Hãy bắt đầu lượt mới!";
        }
        else if (floorNumber > progress.CurrentFloor)
        {
            lockedReason = $"Chưa mở khóa. Bạn cần vượt qua Tầng {progress.CurrentFloor} trước!";
        }
        else if (floorNumber < progress.CurrentFloor)
        {
            lockedReason = "Tầng này đã hoàn thành trong lượt hiện tại.";
        }
        else if (progress.IsCompleted)
        {
            lockedReason = "Bạn đã chinh phục đỉnh tháp hôm nay!";
        }

        return new TowerFloorDetailDto
        {
            FloorNumber = floor.FloorNumber,
            Name = floor.Name,
            FloorType = floor.FloorType,
            RecommendedPower = floor.RecommendedPower,
            BackgroundPath = floor.BackgroundPath,
            State = state,
            HasMilestoneChest = chests != null,
            ChestInfo = chestDto,
            Rewards = ParseRewardsJson(floor.RewardsJson),
            PlayerPower = playerPower,
            EnemyPower = enemyDtos.Sum(e => e.Power),
            Enemies = enemyDtos,
            RemainingLives = progress.RemainingLives,
            MaxLives = ev.InitialLives,
            CanStart = canStart,
            LockedReason = lockedReason
        };
    }
}
