using Core.Common.Errors;
using Core.Persistence.Repositories;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;
using GAME.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Core.Application.CQRS;
using Core.Common.Entity.MyCompany.Shared.Responses;
using GAME.Application.Services;
using GAME.Application.Features.Shared.Tower;
using GAME.Application.Events;

namespace GAME.Application.Features.Commands.Tower;

public sealed record ClaimMilestoneChestCommand(string UserId, int ChestId) : ICommand<BaseResponse<ClaimRewardResultDto>>;

public sealed class ClaimMilestoneChestCommandHandler(
    TowerBattleExecutor executor,
    IUnitOfWork unitOfWork,
    IGamePlayerService gamePlayerService,
    IFormationSnapshotService formationSnapshotService,
    IFormationPowerQueryService formationPowerQueryService,
    ITowerEventService towerEvents,
    TowerOperationRunner runner,
    TowerRewardService rewards,
    IPlayerActivityEvents activityEvents)
    : TowerHandlerSupport(executor, unitOfWork, gamePlayerService, formationSnapshotService, formationPowerQueryService, towerEvents), ICommandHandler<ClaimMilestoneChestCommand, BaseResponse<ClaimRewardResultDto>>
{
    private readonly TowerOperationRunner _runner = runner;
    private readonly TowerRewardService _rewards = rewards;
    public async Task<BaseResponse<ClaimRewardResultDto>> Handle(ClaimMilestoneChestCommand message, CancellationToken ct) => BaseResponse<ClaimRewardResultDto>.SuccessResponse(await ExecuteAsync(message.UserId, message.ChestId, ct));
    private Task<ClaimRewardResultDto> ExecuteAsync(string userId, int chestId, CancellationToken ct = default) => _runner.RunAsync(userId, () => ClaimMilestoneChestAsyncCore(userId, chestId, ct), ct);
    private async Task<ClaimRewardResultDto> ClaimMilestoneChestAsyncCore(string userId, int chestId, CancellationToken ct = default)
    {
        var player = await _gamePlayerService.GetPlayerByUserIdAsync(userId, ct, tracked: true) ?? throw new BusinessException("PLAYER_NOT_FOUND", "Không tìm thấy người chơi.", BusinessErrorKind.NotFound);
        var ev = await GetTowerEventAsync(ct);
        var period = await _towerEvents.GetOrCreateCurrentPeriodAsync(ev, ct);
        var progress = await _towerEvents.GetOrCreatePlayerPeriodProgressAsync(player.Id, ev, period, ct);
        var chest = (await GetPeriodChestsAsync(period, ct)).FirstOrDefault(c => c.Id == chestId) ?? throw new KeyNotFoundException("Không tìm thấy cấu hình rương mốc.");
        if (progress.HighestFloorInPeriod < chest.FloorNumber)
            throw new BusinessException("MILESTONE_NOT_REACHED", $"Chưa đạt mốc Tầng {chest.FloorNumber} trong kỳ hiện tại để mở rương.", BusinessErrorKind.Conflict);
        bool alreadyClaimed = await _unitOfWork.ReadOnlyRepository<HrkPlayerEventRewardClaim>().Query().AnyAsync(c => c.PlayerId == player.Id && c.EventPeriodId == period.Id && c.ClaimType == "MILESTONE_CHEST" && c.TargetId == chest.Id, ct);
        if (alreadyClaimed)
            throw new BusinessException("REWARD_ALREADY_CLAIMED", "Rương này đã được nhận trong kỳ hiện tại.", BusinessErrorKind.Conflict);
        var configuredRewards = ParseRewardsJson(chest.RewardsJson);
        var (granted, pending, bagFull) = await _rewards.GrantGenericRewardsAsync(player.Id, configuredRewards, period.Id, $"Rương mốc Tầng {chest.FloorNumber}", ct);
        var claim = new HrkPlayerEventRewardClaim
        {
            PlayerId = player.Id,
            EventPeriodId = period.Id,
            ClaimType = "MILESTONE_CHEST",
            TargetId = chest.Id,
            ClaimedAtUtc = DateTime.UtcNow,
            RewardSummaryJson = JsonSerializer.Serialize(granted)
        };
        await _unitOfWork.Repository<HrkPlayerEventRewardClaim>().AddAsync(claim);
        activityEvents.RaiseEvent(player, period, "MilestoneChestClaimed", $"{period.Id}:{chest.Id}",
            new { ChestId = chest.Id, chest.FloorNumber, Granted = granted, Pending = pending }, claim.ClaimedAtUtc);
        await _unitOfWork.SaveChangesAsync(ct);
        return new ClaimRewardResultDto
        {
            Success = true,
            Message = bagFull ? "Đã nhận một phần thưởng, các vật phẩm còn lại chuyển vào Thưởng Chờ do túi đầy." : "Nhận rương mốc thành công!",
            ClaimedRewards = granted,
            IsBagFull = bagFull
        };
    }
}
