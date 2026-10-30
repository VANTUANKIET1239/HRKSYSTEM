namespace GAME.Domain.Entities;

public class HrkGameEvent
{
    public int Id { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string EventType { get; set; } = "TOWER";
    public string? Description { get; set; }
    public string BannerImagePath { get; set; } = null!;
    public string? Icon { get; set; }
    public int DisplayOrder { get; set; }
    public int MinPlayerLevel { get; set; } = 1;
    public bool IsOpen { get; set; } = true;
    public DateTime? StartTimeUtc { get; set; }
    public DateTime? EndTimeUtc { get; set; }
    public string ResetType { get; set; } = "DAILY";
    public TimeSpan ResetTime { get; set; } = new TimeSpan(12, 0, 0); // 12:00 PM
    public string TimeZoneId { get; set; } = "Asia/Ho_Chi_Minh";
    public int InitialLives { get; set; } = 3;
    public string LifeConsumeMode { get; set; } = "ON_DEFEAT";
    public int? MaxDailyRuns { get; set; }
    public string? RulesJson { get; set; }
    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedOn { get; set; } = DateTime.UtcNow;

    public virtual ICollection<HrkEventPeriod> Periods { get; set; } = new List<HrkEventPeriod>();
    public virtual ICollection<HrkTowerFloor> Floors { get; set; } = new List<HrkTowerFloor>();
    public virtual ICollection<HrkTowerChestConfig> Chests { get; set; } = new List<HrkTowerChestConfig>();
}
