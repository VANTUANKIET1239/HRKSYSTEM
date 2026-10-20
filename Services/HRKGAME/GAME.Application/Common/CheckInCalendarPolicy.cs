using System.Globalization;
using GAME.Domain.Entities;

namespace GAME.Application.Common;

public static class CheckInCalendarPolicy
{
    public const string Code = "MONTHLY_CHECKIN";

    public static (DateTime StartUtc, DateTime EndUtc, int Days) GetMonthBounds(string monthKey, string timeZoneId, TimeSpan resetTime)
    {
        if (!DateOnly.TryParseExact(monthKey + "-01", "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var month) ||
            monthKey.Length != 7 || resetTime < TimeSpan.Zero || resetTime >= TimeSpan.FromDays(1) ||
            resetTime.Ticks % TimeSpan.TicksPerSecond != 0)
            throw new InvalidOperationException("Invalid monthly check-in calendar.");
        var zone = EventPeriodCalculator.GetEventTimeZone(timeZoneId);
        var start = month.ToDateTime(TimeOnly.MinValue).Add(resetTime);
        return (TimeZoneInfo.ConvertTimeToUtc(start, zone), TimeZoneInfo.ConvertTimeToUtc(start.AddMonths(1), zone),
            DateTime.DaysInMonth(month.Year, month.Month));
    }

    public static (DateOnly GameDate, DateTime NextResetUtc, int Days) Resolve(HrkCheckInCalendar calendar, DateTime now)
    {
        var bounds = GetMonthBounds(calendar.MonthKey, calendar.TimeZoneId, calendar.ResetTime);
        if (calendar.ConfigVersion < 1 || calendar.MinPlayerLevel < 1 ||
            calendar.StartAtUtc != bounds.StartUtc || calendar.EndAtUtc != bounds.EndUtc)
            throw new InvalidOperationException("Check-in calendar boundaries do not match its month and reset settings.");
        var period = EventPeriodCalculator.CalculatePeriodBoundaries(now, calendar.TimeZoneId, calendar.ResetTime);
        var gameDate = DateOnly.ParseExact(period.periodKey, "yyyy-MM-dd", CultureInfo.InvariantCulture);
        if (gameDate.ToString("yyyy-MM", CultureInfo.InvariantCulture) != calendar.MonthKey)
            throw new InvalidOperationException("Check-in date is outside the selected calendar.");
        return (gameDate, period.endUtc, bounds.Days);
    }
}
