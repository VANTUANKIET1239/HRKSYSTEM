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

public class HrkEventPeriod
{
    public long Id { get; set; }
    public int EventId { get; set; }
    public string PeriodKey { get; set; } = null!; // e.g. "2026-10-01"
    public DateTime StartAtUtc { get; set; }
    public DateTime EndAtUtc { get; set; }
    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;

    public virtual HrkGameEvent Event { get; set; } = null!;
    public virtual ICollection<HrkPlayerEventPeriodProgress> PlayerProgresses { get; set; } = new List<HrkPlayerEventPeriodProgress>();
}

public class HrkPlayerEventPeriodProgress
{
    public long Id { get; set; }
    public long PlayerId { get; set; }
    public int EventId { get; set; }
    public long EventPeriodId { get; set; }
    public string PeriodKey { get; set; } = null!;
    public int CurrentFloor { get; set; } = 1;
    public int RemainingLives { get; set; } = 3;
    public int CurrentRunNumber { get; set; } = 1;
    public int QuickClimbRunsUsed { get; set; }
    public int HighestFloorInPeriod { get; set; } = 0;
    public bool IsCompleted { get; set; } = false;
    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedOn { get; set; } = DateTime.UtcNow;

    public virtual HrkPlayer Player { get; set; } = null!;
    public virtual HrkGameEvent Event { get; set; } = null!;
    public virtual HrkEventPeriod EventPeriod { get; set; } = null!;
}

public class HrkPlayerEventAllTimeRecord
{
    public long Id { get; set; }
    public long PlayerId { get; set; }
    public int EventId { get; set; }
    public int HighestFloorAllTime { get; set; } = 0;
    public int TotalClears { get; set; } = 0;
    public DateTime UpdatedOn { get; set; } = DateTime.UtcNow;

    public virtual HrkPlayer Player { get; set; } = null!;
    public virtual HrkGameEvent Event { get; set; } = null!;
}

public class HrkPlayerTowerRun
{
    public long Id { get; set; }
    public long PlayerId { get; set; }
    public long EventPeriodId { get; set; }
    public int RunNumber { get; set; }
    public int StartFloor { get; set; } = 1;
    public int EndFloor { get; set; } = 1;
    public int LivesRemaining { get; set; } = 3;
    public string Status { get; set; } = "IN_PROGRESS"; // IN_PROGRESS, DEFEATED, COMPLETED, EXPIRED
    public DateTime StartAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? EndAtUtc { get; set; }

    public virtual HrkPlayer Player { get; set; } = null!;
    public virtual HrkEventPeriod EventPeriod { get; set; } = null!;
}

public class HrkTowerFloor
{
    public int Id { get; set; }
    public int EventId { get; set; }
    public int FloorNumber { get; set; }
    public string Name { get; set; } = null!;
    public string FloorType { get; set; } = "NORMAL"; // NORMAL, ELITE, BOSS
    public int RecommendedPower { get; set; }
    public string? BackgroundPath { get; set; }
    public int? RepresentativeHeroTemplateId { get; set; }
    public decimal StatMultiplier { get; set; } = 1.0m;
    public decimal HpMultiplier { get; set; } = 1.0m;
    public decimal AtkMultiplier { get; set; } = 1.0m;
    public decimal DefMultiplier { get; set; } = 1.0m;
    public long GoldReward { get; set; }
    public int PlayerExpReward { get; set; }
    public int HeroExpReward { get; set; }
    public string? RewardsJson { get; set; }
    public bool IsActive { get; set; } = true;

    public virtual HrkGameEvent Event { get; set; } = null!;
    public virtual HrkHeroTemplate? RepresentativeHeroTemplate { get; set; }
    public virtual ICollection<HrkTowerFloorEnemy> Enemies { get; set; } = new List<HrkTowerFloorEnemy>();
}

public class HrkTowerFloorEnemy
{
    public long Id { get; set; }
    public int TowerFloorId { get; set; }
    public int Position { get; set; }
    public int HeroTemplateId { get; set; }
    public int Level { get; set; }
    public byte Stars { get; set; } = 1;
    public string? DisplayName { get; set; }
    public string? ImagePath { get; set; }
    public decimal StatMultiplier { get; set; } = 1.0m;

    public virtual HrkTowerFloor TowerFloor { get; set; } = null!;
    public virtual HrkHeroTemplate HeroTemplate { get; set; } = null!;
}

public class HrkTowerChestConfig
{
    public int Id { get; set; }
    public int EventId { get; set; }
    public int FloorNumber { get; set; } // 15, 30, 45, 60
    public string ChestName { get; set; } = null!;
    public string? ChestIcon { get; set; }
    public string? Description { get; set; }
    public long GoldReward { get; set; }
    public int DiamondReward { get; set; }
    public int? GuaranteedItemTemplateId { get; set; }
    public int GuaranteedItemCount { get; set; } = 1;
    public string? RewardsJson { get; set; }
    public bool IsActive { get; set; } = true;

    public virtual HrkGameEvent Event { get; set; } = null!;
    public virtual HrkItemTemplate? GuaranteedItemTemplate { get; set; }
}

public class HrkPlayerEventRewardClaim
{
    public long Id { get; set; }
    public long PlayerId { get; set; }
    public long EventPeriodId { get; set; }
    public string ClaimType { get; set; } = null!; // FLOOR_REWARD, MILESTONE_CHEST, PENDING_REWARD
    public int TargetId { get; set; } // FloorNumber or ChestId
    public DateTime ClaimedAtUtc { get; set; } = DateTime.UtcNow;
    public string? RewardSummaryJson { get; set; }

    public virtual HrkPlayer Player { get; set; } = null!;
    public virtual HrkEventPeriod EventPeriod { get; set; } = null!;
}

public class HrkPlayerPendingReward
{
    public long Id { get; set; }
    public long PlayerId { get; set; }
    public long EventPeriodId { get; set; }
    public string SourceType { get; set; } = "UNCLAIMED_MILESTONE_CHEST"; // UNCLAIMED_MILESTONE_CHEST, BAG_FULL_REWARD
    public int SourceRefId { get; set; } // FloorNumber or ChestId
    public string Description { get; set; } = null!;
    public string RewardItemsJson { get; set; } = null!;
    public string Status { get; set; } = "PENDING"; // PENDING, CLAIMED, EXPIRED
    public DateTime CreatedOnUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ClaimedOnUtc { get; set; }

    public virtual HrkPlayer Player { get; set; } = null!;
    public virtual HrkEventPeriod EventPeriod { get; set; } = null!;
}

public class HrkTowerQuickClimbJob
{
    public long Id { get; set; }
    public string JobId { get; set; } = null!;
    public long PlayerId { get; set; }
    public long EventPeriodId { get; set; }
    public string Status { get; set; } = "QUEUED"; // QUEUED, PROCESSING, COMPLETED, STOPPED_DEFEAT, CANCELLED, EXPIRED, ERROR
    public string? StopReason { get; set; } // FIRST_DEFEAT, TOWER_COMPLETED, USER_CANCELLED, PERIOD_EXPIRED, EVENT_CLOSED, RULES_CHANGED, ERROR
    public int StartFloor { get; set; }
    public int CurrentFloor { get; set; }
    public int TargetFloor { get; set; }
    public int InitialLives { get; set; }
    public int RemainingLives { get; set; }
    public int ClearedFloorsCount { get; set; } = 0;
    public int DailyRunNumber { get; set; }
    public int? FailedFloor { get; set; }
    public string FormationCode { get; set; } = null!;
    public string FormationSnapshotJson { get; set; } = null!;
    public string? AccumulatedRewardsJson { get; set; }
    public string? LogsJson { get; set; }
    public string? LastBattleId { get; set; }
    public string? WorkerId { get; set; }
    public DateTime? LockedUntilUtc { get; set; }
    public DateTime CreatedOnUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedOnUtc { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedOnUtc { get; set; }
    public long Version { get; set; }

    public virtual HrkPlayer Player { get; set; } = null!;
    public virtual HrkEventPeriod EventPeriod { get; set; } = null!;
}

public class HrkPlayerTowerBattle
{
    public long Id { get; set; }
    public string BattleId { get; set; } = null!;
    public long PlayerId { get; set; }
    public long EventPeriodId { get; set; }
    public int FloorNumber { get; set; }
    public long? RunId { get; set; }
    public long? QuickClimbJobId { get; set; }
    public string Result { get; set; } = null!; // VICTORY, DEFEAT
    public int LivesBefore { get; set; }
    public int LivesAfter { get; set; }
    public int PlayerPower { get; set; }
    public int EnemyPower { get; set; }
    public int RandomSeed { get; set; }
    public string? ResultJson { get; set; }
    public string? HeroStatisticsJson { get; set; }
    public DateTime CreatedOnUtc { get; set; } = DateTime.UtcNow;

    public virtual HrkPlayer Player { get; set; } = null!;
    public virtual HrkEventPeriod EventPeriod { get; set; } = null!;
}
