namespace GAME.Application.DTOs;

public sealed record ClaimCheckInRequestDto(Guid RequestId);
public sealed record CheckInDayDto(int DayOfMonth, string Status, IReadOnlyList<GenericRewardItemDto> Rewards, DateTime? ClaimedAtUtc);
public sealed record CheckInCalendarDto(long? CalendarId, string Name, string? MonthKey, DateOnly? GameDate,
    DateTime ServerTimeUtc, DateTime? NextResetAtUtc, bool IsOpen, bool CanClaim, string? Reason,
    int ClaimedDays, IReadOnlyList<CheckInDayDto> Days);
public sealed record CheckInClaimResultDto(Guid RequestId, long CalendarId, DateOnly CheckInDate,
    DateTime ClaimedAtUtc, int ConfigVersion, bool AlreadyClaimed, IReadOnlyList<GenericRewardItemDto> Rewards);
