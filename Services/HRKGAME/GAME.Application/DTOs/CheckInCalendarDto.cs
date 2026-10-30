namespace GAME.Application.DTOs;
public sealed record CheckInCalendarDto(long? CalendarId, string Name, string? MonthKey, DateOnly? GameDate,
    DateTime ServerTimeUtc, DateTime? NextResetAtUtc, bool IsOpen, bool CanClaim, string? Reason,
    int ClaimedDays, IReadOnlyList<CheckInDayDto> Days)
{
    public string? ReasonKey { get; init; }
    public IReadOnlyDictionary<string, int>? ReasonArguments { get; init; }
}
