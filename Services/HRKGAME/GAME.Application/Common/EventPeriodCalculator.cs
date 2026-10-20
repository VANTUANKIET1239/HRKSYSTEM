using System.Globalization;

namespace GAME.Application.Common;

/// <summary>Pure period/time calculations shared by game events; no persistence or gameplay state.</summary>
public static class EventPeriodCalculator
{
    public static TimeZoneInfo GetEventTimeZone(string timeZoneId)
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
        catch (Exception exception) when (exception is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            if (timeZoneId.Contains("Ho_Chi_Minh", StringComparison.OrdinalIgnoreCase) ||
                timeZoneId.Contains("Bangkok", StringComparison.OrdinalIgnoreCase) ||
                timeZoneId.Contains("Hanoi", StringComparison.OrdinalIgnoreCase))
            {
                try { return TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"); }
                catch (Exception fallback) when (fallback is TimeZoneNotFoundException or InvalidTimeZoneException)
                {
                    return TimeZoneInfo.CreateCustomTimeZone("Asia/Ho_Chi_Minh", TimeSpan.FromHours(7), "Indochina Time", "Indochina Time");
                }
            }
            throw new InvalidOperationException($"Invalid event time zone: {timeZoneId}", exception);
        }
    }

    public static (DateTime startUtc, DateTime endUtc, string periodKey) CalculatePeriodBoundaries(
        DateTime utcNow, string timeZoneId, TimeSpan resetTime)
    {
        if (resetTime < TimeSpan.Zero || resetTime >= TimeSpan.FromDays(1))
            throw new ArgumentOutOfRangeException(nameof(resetTime), "Reset time must be within one day.");
        var timeZone = GetEventTimeZone(timeZoneId);
        var localTime = TimeZoneInfo.ConvertTimeFromUtc(utcNow, timeZone);
        var localStart = localTime.Date.Add(resetTime);
        if (localTime.TimeOfDay < resetTime) localStart = localStart.AddDays(-1);
        return (
            TimeZoneInfo.ConvertTimeToUtc(localStart, timeZone),
            TimeZoneInfo.ConvertTimeToUtc(localStart.AddDays(1), timeZone),
            localStart.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
    }

    public static long CalculateRemainingSecondsToReset(DateTime endAtUtc, DateTime? utcNow = null) =>
        (long)Math.Max(0, (endAtUtc - (utcNow ?? DateTime.UtcNow)).TotalSeconds);
}
