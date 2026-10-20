namespace GAME.Domain.Entities;

public sealed class HrkCheckInCalendar
{
    public long Id { get; set; }
    public string Code { get; set; } = "MONTHLY_CHECKIN";
    public string Name { get; set; } = "Điểm danh tháng";
    public string MonthKey { get; set; } = null!;
    public string TimeZoneId { get; set; } = "Asia/Ho_Chi_Minh";
    public TimeSpan ResetTime { get; set; }
    public DateTime StartAtUtc { get; set; }
    public DateTime EndAtUtc { get; set; }
    public int MinPlayerLevel { get; set; } = 1;
    public int ConfigVersion { get; set; } = 1;
    public bool IsPublished { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class HrkCheckInRewardConfig
{
    public long Id { get; set; }
    public long CalendarId { get; set; }
    public int DayOfMonth { get; set; }
    public string RewardsJson { get; set; } = "[]";
}

public sealed class HrkPlayerCheckInClaim
{
    public long Id { get; set; }
    public long PlayerId { get; set; }
    public long CalendarId { get; set; }
    public DateOnly CheckInDate { get; set; }
    public Guid RequestId { get; set; }
    public int ConfigVersion { get; set; }
    public DateTime ClaimedAtUtc { get; set; }
    // Full receipt: retries and historical calendar cells use the reward actually granted.
    public string ResultJson { get; set; } = null!;
}
