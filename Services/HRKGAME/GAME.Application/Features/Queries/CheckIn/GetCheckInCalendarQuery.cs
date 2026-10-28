using System.Text.Json;
using Core.Application.CQRS;
using Core.Common.Entity.MyCompany.Shared.Responses;
using Core.Common.Errors;
using Core.Persistence.Repositories;
using GAME.Application.DTOs;
using GAME.Application.Features.Shared.CheckIn;
using GAME.Application.Interfaces;
using GAME.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GAME.Application.Features.Queries.CheckIn;

public sealed record GetCheckInCalendarQuery(string UserId) : IQuery<BaseResponse<CheckInCalendarDto>>;

public sealed class GetCheckInCalendarQueryHandler(IUnitOfWork unitOfWork, IGamePlayerService players, TimeProvider clock)
    : IQueryHandler<GetCheckInCalendarQuery, BaseResponse<CheckInCalendarDto>>
{
    public async Task<BaseResponse<CheckInCalendarDto>> Handle(GetCheckInCalendarQuery request, CancellationToken ct)
    {
        var player = await players.GetPlayerByUserIdAsync(request.UserId, ct) ??
            throw new BusinessException("PLAYER_NOT_FOUND", "Không tìm thấy người chơi.", BusinessErrorKind.NotFound);
        var now = clock.GetUtcNow().UtcDateTime;
        var current = await CheckInCalendarReader.LoadAsync(unitOfWork, now, ct);
        if (current is null)
            return BaseResponse<CheckInCalendarDto>.SuccessResponse(new(null, "Điểm danh tháng", null, null, now, null,
                false, false, "Lịch điểm danh tháng này chưa mở.", 0, []));
        var calendar = current.Calendar;
        var claims = await unitOfWork.ReadOnlyRepository<HrkPlayerCheckInClaim>().Query()
            .Where(x => x.PlayerId == player.Id && x.CalendarId == calendar.Id).ToListAsync(ct);
        var byDay = claims.ToDictionary(x => x.CheckInDate.Day);
        var unlocked = player.Level >= calendar.MinPlayerLevel;
        var days = current.Days.OrderBy(x => x.Key).Select(x =>
        {
            if (byDay.TryGetValue(x.Key, out var claim))
            {
                var receipt = JsonSerializer.Deserialize<CheckInClaimResultDto>(claim.ResultJson) ??
                    throw new InvalidOperationException("Invalid saved check-in receipt.");
                return new CheckInDayDto(x.Key, "CLAIMED", receipt.Rewards, claim.ClaimedAtUtc);
            }
            var status = x.Key < current.GameDate.Day ? "MISSED" :
                x.Key == current.GameDate.Day && unlocked ? "CLAIMABLE" : "LOCKED";
            return new CheckInDayDto(x.Key, status, x.Value.Rewards, null);
        }).ToArray();
        return BaseResponse<CheckInCalendarDto>.SuccessResponse(new(calendar.Id, calendar.Name, calendar.MonthKey,
            current.GameDate, now, current.NextResetUtc, true, unlocked && !byDay.ContainsKey(current.GameDate.Day),
            unlocked ? null : $"Cần đạt cấp {calendar.MinPlayerLevel} để điểm danh.", claims.Count, days)
        {
            ReasonKey = unlocked ? null : "CHECKIN_LEVEL_REQUIRED",
            ReasonArguments = unlocked ? null : new Dictionary<string, int> { ["level"] = calendar.MinPlayerLevel }
        });
    }
}
