using System.Text.Json;
using System.Globalization;
using Core.Application.CQRS;
using Core.Common.Entity.MyCompany.Shared.Responses;
using Core.Common.Errors;
using Core.Persistence.Repositories;
using GAME.Application.Common;
using GAME.Application.DTOs;
using GAME.Application.Events;
using GAME.Application.Features.Shared.CheckIn;
using GAME.Application.Interfaces;
using GAME.Application.Services;
using GAME.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GAME.Application.Features.Commands.CheckIn;

public sealed class ClaimCheckInCommandHandler(IUnitOfWork unitOfWork, IGamePlayerService players,
    GameRewardGranter rewards, IPlayerActivityEvents activities, TimeProvider clock, IGameTransactionLock locks)
    : ICommandHandler<ClaimCheckInCommand, BaseResponse<CheckInClaimResultDto>>
{
    public async Task<BaseResponse<CheckInClaimResultDto>> Handle(ClaimCheckInCommand request, CancellationToken ct)
    {
        if (request.RequestId == Guid.Empty)
            throw new BusinessException("INVALID_REQUEST_ID", "Mã yêu cầu điểm danh không hợp lệ.", BusinessErrorKind.Validation);
        CheckInClaimResultDto? result = null;
        await unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var player = await players.GetPlayerByUserIdAsync(request.UserId, ct) ??
                throw new BusinessException("PLAYER_NOT_FOUND", "Không tìm thấy người chơi.", BusinessErrorKind.NotFound);
            // Same wallet lock used by summon. Serializes check-in receipts across instances and protects currency updates.
            await locks.LockWalletAsync(player.Id, ct);
            var previous = await unitOfWork.ReadOnlyRepository<HrkPlayerCheckInClaim>().Query()
                .SingleOrDefaultAsync(x => x.PlayerId == player.Id && x.RequestId == request.RequestId, ct);
            if (previous is not null) { result = Replay(previous); return; }
            var now = clock.GetUtcNow().UtcDateTime;
            var current = await CheckInCalendarReader.LoadAsync(unitOfWork, now, ct) ??
                throw new BusinessException("CHECKIN_CLOSED", "Lịch điểm danh tháng này chưa mở.", BusinessErrorKind.Conflict);
            previous = await unitOfWork.ReadOnlyRepository<HrkPlayerCheckInClaim>().Query()
                .SingleOrDefaultAsync(x => x.PlayerId == player.Id && x.CalendarId == current.Calendar.Id && x.CheckInDate == current.GameDate, ct);
            if (previous is not null) { result = Replay(previous); return; }
            if (player.Level < current.Calendar.MinPlayerLevel)
                throw new BusinessException("CHECKIN_LEVEL_REQUIRED", $"Cần đạt cấp {current.Calendar.MinPlayerLevel} để điểm danh.", BusinessErrorKind.Conflict)
                { Arguments = new Dictionary<string, object?> { ["level"] = current.Calendar.MinPlayerLevel } };
            var day = current.Days[current.GameDate.Day];
            var (granted, pending, _) = await rewards.GrantAsync(player.Id, day.Rewards, "CHECK_IN", ct);
            if (pending.Count > 0)
                throw new BusinessException("INVENTORY_FULL", "Hành trang không đủ chỗ. Hãy dọn túi rồi nhận lại phần thưởng điểm danh.", BusinessErrorKind.Conflict);
            result = new(request.RequestId, current.Calendar.Id, current.GameDate, now, current.Calendar.ConfigVersion, false, granted);
            await unitOfWork.Repository<HrkPlayerCheckInClaim>().AddAsync(new()
            {
                PlayerId = player.Id, CalendarId = current.Calendar.Id, CheckInDate = current.GameDate,
                RequestId = request.RequestId, ConfigVersion = current.Calendar.ConfigVersion, ClaimedAtUtc = now,
                ResultJson = JsonSerializer.Serialize(result)
            });
            activities.Raise(new CheckInClaimedEvent(player.Id, player.UserId, current.Calendar.Id, current.Calendar.MonthKey,
                current.GameDate, request.RequestId, current.Calendar.ConfigVersion,
                granted.Select(x => new { x.Type, x.Code, x.ItemTemplateId, x.Quantity }).ToArray())
            { PeriodKey = current.GameDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), OccurredAt = new DateTimeOffset(now) });
        }, ct);
        return BaseResponse<CheckInClaimResultDto>.SuccessResponse(result!);
    }

    private static CheckInClaimResultDto Replay(HrkPlayerCheckInClaim claim) =>
        (JsonSerializer.Deserialize<CheckInClaimResultDto>(claim.ResultJson) ??
         throw new InvalidOperationException("Invalid saved check-in receipt.")) with { AlreadyClaimed = true };
}
