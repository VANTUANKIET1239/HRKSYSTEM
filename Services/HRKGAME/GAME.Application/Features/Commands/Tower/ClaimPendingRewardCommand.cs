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
using System.Security.Cryptography;
using System.Text;

namespace GAME.Application.Features.Commands.Tower;

public sealed record ClaimPendingRewardCommand(string UserId, long PendingRewardId) : ICommand<BaseResponse<ClaimRewardResultDto>>;

public sealed class ClaimPendingRewardCommandHandler(
    TowerBattleExecutor executor,
    IUnitOfWork unitOfWork,
    IGamePlayerService gamePlayerService,
    IFormationSnapshotService formationSnapshotService,
    IFormationPowerQueryService formationPowerQueryService,
    ITowerEventService towerEvents,
    TowerOperationRunner runner,
    TowerRewardService rewards,
    IPlayerActivityEvents activityEvents)
    : TowerHandlerSupport(executor, unitOfWork, gamePlayerService, formationSnapshotService, formationPowerQueryService, towerEvents), ICommandHandler<ClaimPendingRewardCommand, BaseResponse<ClaimRewardResultDto>>
{
    private readonly TowerOperationRunner _runner = runner;
    private readonly TowerRewardService _rewards = rewards;
    public async Task<BaseResponse<ClaimRewardResultDto>> Handle(ClaimPendingRewardCommand message, CancellationToken ct) => BaseResponse<ClaimRewardResultDto>.SuccessResponse(await ExecuteAsync(message.UserId, message.PendingRewardId, ct));
    private Task<ClaimRewardResultDto> ExecuteAsync(string userId, long pendingRewardId, CancellationToken ct = default) => _runner.RunAsync(userId, () => ClaimPendingRewardAsyncCore(userId, pendingRewardId, ct), ct);
    private async Task<ClaimRewardResultDto> ClaimPendingRewardAsyncCore(string userId, long pendingRewardId, CancellationToken ct = default)
    {
        var player = await _gamePlayerService.GetPlayerByUserIdAsync(userId, ct, tracked: true) ?? throw new BusinessException("PLAYER_NOT_FOUND", "Không tìm thấy người chơi.", BusinessErrorKind.NotFound);
        var pending = await _unitOfWork.Repository<HrkPlayerPendingReward>().Query()
            .FirstOrDefaultAsync(r => r.Id == pendingRewardId && r.PlayerId == player.Id && r.Status == "PENDING", ct) ?? throw new BusinessException("REWARD_NOT_FOUND", "Không tìm thấy phần thưởng chờ hợp lệ.", BusinessErrorKind.NotFound);
        var rewards = ParseRewardsJson(pending.RewardItemsJson);
        var rewardVersion = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(pending.RewardItemsJson)));
        var (granted, stillPending, bagFull) = await _rewards.GrantGenericRewardsAsync(player.Id, rewards, pending.EventPeriodId, pending.Description, ct, createPending: false);
        if (granted.Count > 0)
        {
            var period = await _unitOfWork.ReadOnlyRepository<HrkEventPeriod>().Query().SingleAsync(x => x.Id == pending.EventPeriodId, ct);
            activityEvents.RaiseEvent(player, period, "PendingRewardClaimed", $"{pending.Id}:{rewardVersion}",
                new { PendingRewardId = pending.Id, Granted = granted, Remaining = stillPending });
        }
        if (stillPending.Count == 0)
        {
            pending.Status = "CLAIMED";
            pending.ClaimedOnUtc = DateTime.UtcNow;
            await _unitOfWork.SaveChangesAsync(ct);
            return new ClaimRewardResultDto
            {
                Success = true,
                Message = "Đã nhận toàn bộ phần thưởng chờ!",
                ClaimedRewards = granted,
                IsBagFull = false
            };
        }
        else
        {
            pending.RewardItemsJson = JsonSerializer.Serialize(stillPending);
            await _unitOfWork.SaveChangesAsync(ct);
            return new ClaimRewardResultDto
            {
                Success = granted.Count > 0,
                Message = "Hành trang đầy. Vui lòng dọn dẹp để nhận tiếp phần còn lại.",
                ClaimedRewards = granted,
                IsBagFull = true
            };
        }
    }
}
